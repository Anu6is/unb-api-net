using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using UnbelievaBoat.Net.Models;
using System.Text.Json.Serialization; // Required for JsonIgnoreCondition

namespace UnbelievaBoat.Net
{
    public class UnbelievaBoatClient : IDisposable
    {
        private readonly HttpClient _internalHttpClient; // Renamed from _httpClient
        private readonly bool _isExternalClient;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
        private readonly string _apiToken;
        private readonly string _baseUrl; // Store base URL for potential use or logging

        private const string DefaultApiBaseUrl = "https://unbelievaboat.com/api/v1";

        public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, // For deserialization matching
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, // For serialization to snake_case
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // For PATCH requests primarily
        };

        // Inner class for GuildLeaderboardOptions
        public class GuildLeaderboardOptions
        {
            public int? Limit { get; set; }
            public int? Offset { get; set; }
            /// <summary>
            /// Sorts by the specified field. Valid options are "cash", "bank", and "total".
            /// </summary>
            public string Sort { get; set; }
        }

        private int? _lastRateLimitLimit;
        private int? _lastRateLimitRemaining;
        private TimeSpan? _lastRateLimitResetAfter;
        private DateTime? _rateLimitResetTimeUtc;
        private static readonly SemaphoreSlim _rateLimitSemaphore = new SemaphoreSlim(1, 1);

        public int? LastRateLimitLimit => _lastRateLimitLimit;
        public int? LastRateLimitRemaining => _lastRateLimitRemaining;
        public DateTime? LastRateLimitResetTimeUtc => _rateLimitResetTimeUtc;

        // Inner class for StoreItemsQueryOptions
        public class StoreItemsQueryOptions
        {
            public int? Limit { get; set; }
            public int? Offset { get; set; }
            /// <summary>
            /// Sorts by the specified field. e.g., "name", "price", "priority", "created_at", "updated_at".
            /// Prepend with '-' for descending order (e.g., "-price").
            /// </summary>
            public string Sort { get; set; }
            public bool? Hidden { get; set; } // Filter by hidden status
        }

        // Inner class for UserInventoryQueryOptions
        public class UserInventoryQueryOptions
        {
            public int? Limit { get; set; }
            public int? Offset { get; set; }
            /// <summary>
            /// Sorts by the specified field. e.g., "name", "quantity", "added_at".
            /// Prepend with '-' for descending order.
            /// </summary>
            public string Sort { get; set; }
            /// <summary>
            /// Filters by a specific store item ID.
            /// </summary>
            public string StoreItemId { get; set; } // Corresponds to 'item_id' query parameter
        }

        // Inner class for RemoveUserInventoryItemOptions
        public class RemoveUserInventoryItemOptions
        {
            /// <summary>
            /// The number of items to remove. If not specified, API might remove all or default to 1.
            /// </summary>
            public int? Quantity { get; set; }
            /// <summary>
            /// The specific inventory item instance ID to remove from.
            /// This is used if a user can have multiple distinct stacks of the same store item.
            /// </summary>
            public string InventoryInstanceId { get; set; } // Corresponds to 'inventory_item_id' query parameter
        }

        public UnbelievaBoatClient(string apiToken, string baseUrl = DefaultApiBaseUrl, RetryPolicyConfig retryConfig = null, HttpClient httpClient = null)
        {
            if (string.IsNullOrWhiteSpace(apiToken)) throw new ArgumentNullException(nameof(apiToken));
            if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentNullException(nameof(baseUrl));

            _apiToken = apiToken;
            _baseUrl = baseUrl;

            if (httpClient != null)
            {
                _internalHttpClient = httpClient;
                _isExternalClient = true;
                if (_internalHttpClient.BaseAddress == null)
                {
                    _internalHttpClient.BaseAddress = new Uri(_baseUrl);
                }
                _internalHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
            }
            else
            {
                _internalHttpClient = new HttpClient();
                _internalHttpClient.BaseAddress = new Uri(_baseUrl);
                _internalHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
                _isExternalClient = false;
            }
            _internalHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


            var effectiveConfig = retryConfig ?? new RetryPolicyConfig();
            if (effectiveConfig.MaxRetries > 0)
            {
                var transientHttpStatusCodes = new[] { HttpStatusCode.InternalServerError, HttpStatusCode.BadGateway, HttpStatusCode.ServiceUnavailable, HttpStatusCode.GatewayTimeout };
                var policyBuilder = Policy.Handle<HttpRequestException>()
                                          .OrResult<HttpResponseMessage>(r => transientHttpStatusCodes.Contains(r.StatusCode));
                if (effectiveConfig.RetryOnRateLimitExceeded)
                {
                    policyBuilder = policyBuilder.OrResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.TooManyRequests);
                }
                _retryPolicy = policyBuilder.WaitAndRetryAsync(
                    effectiveConfig.MaxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Min(effectiveConfig.InitialDelay.TotalSeconds * Math.Pow(effectiveConfig.BackoffFactor, retryAttempt - 1), effectiveConfig.MaxDelay.TotalSeconds))
                    //,(outcome, timespan, retryAttempt, context) => { /* Optional: Log onRetry */ }
                );
            }
            else
            {
                _retryPolicy = Policy.NoOpAsync<HttpResponseMessage>();
            }
        }

        private async Task<HttpResponseMessage> SendHttpRequestAsync(Func<Task<HttpResponseMessage>> requestFactory, string endpointIdentifier)
        {
            HttpResponseMessage httpResponseMessage;
            await _rateLimitSemaphore.WaitAsync();
            try
            {
                if (_lastRateLimitRemaining.HasValue && _lastRateLimitRemaining <= 1 && _rateLimitResetTimeUtc.HasValue && _rateLimitResetTimeUtc > DateTime.UtcNow)
                {
                    var delay = _rateLimitResetTimeUtc.Value - DateTime.UtcNow;
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay);
                    }
                }

                httpResponseMessage = await _retryPolicy.ExecuteAsync(async () =>
                {
                    var response = await requestFactory();
                    return response;
                });

                if (httpResponseMessage != null)
                {
                    if (httpResponseMessage.Headers.TryGetValues("X-RateLimit-Limit", out var limitValues) && int.TryParse(limitValues.FirstOrDefault(), out int limit))
                    {
                        _lastRateLimitLimit = limit;
                    }

                    if (httpResponseMessage.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues) && int.TryParse(remainingValues.FirstOrDefault(), out int remaining))
                    {
                        _lastRateLimitRemaining = remaining;
                    }

                    if (httpResponseMessage.Headers.TryGetValues("X-RateLimit-Reset-After", out var resetAfterValues) && double.TryParse(resetAfterValues.FirstOrDefault(), out double resetAfterSeconds))
                    {
                        _lastRateLimitResetAfter = TimeSpan.FromSeconds(resetAfterSeconds);
                        _rateLimitResetTimeUtc = DateTime.UtcNow + _lastRateLimitResetAfter.Value;
                    }
                }
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }
            return httpResponseMessage;
        }

        // Balance Endpoints
        public async Task<UserBalance> GetUserBalanceAsync(string guildId, string userId)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));

            var endpoint = $"/guilds/{guildId}/users/{userId}";
            return await SendGetRequestAsync<UserBalance>(endpoint);
        }

        public async Task<UserBalance> SetUserBalanceAsync(string guildId, string userId, UpdateUserBalanceRequest payload)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            var endpoint = $"/guilds/{guildId}/users/{userId}";
            return await SendPutRequestAsync<UpdateUserBalanceRequest, UserBalance>(endpoint, payload);
        }

        public async Task<UserBalance> UpdateUserBalanceAsync(string guildId, string userId, UpdateUserBalanceRequest payload)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            var endpoint = $"/guilds/{guildId}/users/{userId}";
            return await SendPatchRequestAsync<UpdateUserBalanceRequest, UserBalance>(endpoint, payload);
        }

        public async Task<GuildLeaderboard> GetGuildLeaderboardAsync(string guildId, GuildLeaderboardOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));

            var endpoint = $"/guilds/{guildId}/users";

            if (options != null)
            {
                var queryParams = new Dictionary<string, string>();
                if (options.Limit.HasValue) queryParams["limit"] = options.Limit.Value.ToString();
                if (options.Offset.HasValue) queryParams["offset"] = options.Offset.Value.ToString();
                if (!string.IsNullOrWhiteSpace(options.Sort)) queryParams["sort"] = options.Sort;

                if (queryParams.Any())
                {
                    var queryString = string.Join("&", queryParams
                        .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                    endpoint += $"?{queryString}";
                }
            }
            return await SendGetRequestAsync<GuildLeaderboard>(endpoint);
        }

        // Guild Endpoints
        public async Task<Guild> GetGuildAsync(string guildId)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));

            var endpoint = $"/guilds/{guildId}";
            return await SendGetRequestAsync<Guild>(endpoint);
        }

        // Application Endpoints
        public async Task<ApplicationPermissions> GetApplicationPermissionsAsync()
        {
            var endpoint = "/applications/@me/permissions";
            return await SendGetRequestAsync<ApplicationPermissions>(endpoint);
        }

        // Store Item Endpoints
        public async Task<List<StoreItem>> GetStoreItemsAsync(string guildId, StoreItemsQueryOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));

            var endpoint = $"/guilds/{guildId}/items";

            if (options != null)
            {
                var queryParams = new Dictionary<string, string>();
                if (options.Limit.HasValue) queryParams["limit"] = options.Limit.Value.ToString();
                if (options.Offset.HasValue) queryParams["offset"] = options.Offset.Value.ToString();
                if (!string.IsNullOrWhiteSpace(options.Sort)) queryParams["sort"] = options.Sort;
                if (options.Hidden.HasValue) queryParams["hidden"] = options.Hidden.Value.ToString().ToLowerInvariant();

                if (queryParams.Any())
                {
                    var queryString = string.Join("&", queryParams
                        .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                    endpoint += $"?{queryString}";
                }
            }
            return await SendGetRequestAsync<List<StoreItem>>(endpoint);
        }

        public async Task<StoreItem> GetStoreItemAsync(string guildId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item ID cannot be null or whitespace.", nameof(itemId));

            var endpoint = $"/guilds/{guildId}/items/{itemId}";
            return await SendGetRequestAsync<StoreItem>(endpoint);
        }

        public async Task<StoreItem> CreateStoreItemAsync(string guildId, StoreItem itemToCreate)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (itemToCreate == null) throw new ArgumentNullException(nameof(itemToCreate));

            var endpoint = $"/guilds/{guildId}/items";
            return await SendPostRequestAsync<StoreItem, StoreItem>(endpoint, itemToCreate);
        }

        public async Task<StoreItem> EditStoreItemAsync(string guildId, string itemId, StoreItem itemUpdates)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item ID cannot be null or whitespace.", nameof(itemId));
            if (itemUpdates == null) throw new ArgumentNullException(nameof(itemUpdates));

            var endpoint = $"/guilds/{guildId}/items/{itemId}";
            return await SendPatchRequestAsync<StoreItem, StoreItem>(endpoint, itemUpdates);
        }

        public async Task DeleteStoreItemAsync(string guildId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item ID cannot be null or whitespace.", nameof(itemId));

            var endpoint = $"/guilds/{guildId}/items/{itemId}";
            await SendDeleteRequestAsync(endpoint);
        }

        // User Inventory Endpoints
        public async Task<List<InventoryItem>> GetUserInventoryItemsAsync(string guildId, string userId, UserInventoryQueryOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));

            var endpoint = $"/users/{userId}/guilds/{guildId}/inventory";

            if (options != null)
            {
                var queryParams = new Dictionary<string, string>();
                if (options.Limit.HasValue) queryParams["limit"] = options.Limit.Value.ToString();
                if (options.Offset.HasValue) queryParams["offset"] = options.Offset.Value.ToString();
                if (!string.IsNullOrWhiteSpace(options.Sort)) queryParams["sort"] = options.Sort;
                if (!string.IsNullOrWhiteSpace(options.StoreItemId)) queryParams["item_id"] = options.StoreItemId;

                if (queryParams.Any())
                {
                    var queryString = string.Join("&", queryParams
                        .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                    endpoint += $"?{queryString}";
                }
            }
            return await SendGetRequestAsync<List<InventoryItem>>(endpoint);
        }

        public async Task<InventoryItem> GetUserInventoryItemAsync(string guildId, string userId, string inventoryItemId)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));
            if (string.IsNullOrWhiteSpace(inventoryItemId)) throw new ArgumentException("Inventory Item ID cannot be null or whitespace.", nameof(inventoryItemId));

            var endpoint = $"/users/{userId}/guilds/{guildId}/inventory/{inventoryItemId}";
            return await SendGetRequestAsync<InventoryItem>(endpoint);
        }

        public async Task<InventoryItem> AddUserInventoryItemAsync(string guildId, string userId, string storeItemId, AddInventoryItemRequest payload)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));
            if (string.IsNullOrWhiteSpace(storeItemId)) throw new ArgumentException("Store Item ID cannot be null or whitespace.", nameof(storeItemId));
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (payload.Quantity.HasValue && payload.Quantity.Value <= 0) throw new ArgumentOutOfRangeException(nameof(payload.Quantity), "Quantity to add must be positive.");

            var endpoint = $"/users/{userId}/guilds/{guildId}/inventory/{storeItemId}";
            return await SendPostRequestAsync<AddInventoryItemRequest, InventoryItem>(endpoint, payload);
        }

        public async Task<InventoryItem> RemoveUserInventoryItemAsync(string guildId, string userId, string storeItemId, RemoveUserInventoryItemOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));
            if (string.IsNullOrWhiteSpace(storeItemId)) throw new ArgumentException("Store Item ID cannot be null or whitespace.", nameof(storeItemId));

            var endpoint = $"/users/{userId}/guilds/{guildId}/inventory/{storeItemId}";
            var queryParams = new Dictionary<string, string>();

            if (options != null)
            {
                if (options.Quantity.HasValue)
                {
                    if (options.Quantity.Value <= 0) throw new ArgumentOutOfRangeException(nameof(options.Quantity), "Quantity to remove must be positive.");
                    queryParams["quantity"] = options.Quantity.Value.ToString();
                }
                if (!string.IsNullOrWhiteSpace(options.InventoryInstanceId))
                {
                    queryParams["inventory_item_id"] = options.InventoryInstanceId;
                }
            }

            if (queryParams.Any())
            {
                var queryString = string.Join("&", queryParams
                    .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                endpoint += $"?{queryString}";
            }
            return await SendDeleteRequestWithResponseAsync<InventoryItem>(endpoint);
        }

        // Generic Send Methods
        private async Task<TResponse> DeserializeResponseAsync<TResponse>(HttpResponseMessage responseMessage)
        {
            string responseBody = await responseMessage.Content.ReadAsStringAsync();
            responseMessage.Content?.Dispose();
            return JsonSerializer.Deserialize<TResponse>(responseBody, DefaultJsonSerializerOptions);
        }

        private async Task<TResponse> SendGetRequestAsync<TResponse>(string endpoint)
        {
            HttpResponseMessage response = await SendHttpRequestAsync(() => _internalHttpClient.GetAsync(endpoint), endpoint);
            await EnsureSuccessStatusCodeAsync(response);
            return await DeserializeResponseAsync<TResponse>(response);
        }

        private async Task<TResponse> SendPostRequestAsync<TRequest, TResponse>(string endpoint, TRequest payload)
        {
            string jsonPayload = JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions);
            using StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await SendHttpRequestAsync(() => _internalHttpClient.PostAsync(endpoint, content), endpoint);
            await EnsureSuccessStatusCodeAsync(response);
            return await DeserializeResponseAsync<TResponse>(response);
        }

        private async Task SendPostRequestAsync<TRequest>(string endpoint, TRequest payload)
        {
            string jsonPayload = JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions);
            using StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await SendHttpRequestAsync(() => _internalHttpClient.PostAsync(endpoint, content), endpoint);
            await EnsureSuccessStatusCodeAsync(response);
            response.Content?.Dispose();
        }

        private async Task<TResponse> SendPutRequestAsync<TRequest, TResponse>(string endpoint, TRequest payload)
        {
            string jsonPayload = JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions);
            using StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await SendHttpRequestAsync(() => _internalHttpClient.PutAsync(endpoint, content), endpoint);
            await EnsureSuccessStatusCodeAsync(response);
            return await DeserializeResponseAsync<TResponse>(response);
        }

        private async Task SendPutRequestAsync<TRequest>(string endpoint, TRequest payload)
        {
            string jsonPayload = JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions);
            using StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await SendHttpRequestAsync(() => _internalHttpClient.PutAsync(endpoint, content), endpoint);
            await EnsureSuccessStatusCodeAsync(response);
            response.Content?.Dispose();
        }

        private async Task<TResponse> SendPatchRequestAsync<TRequest, TResponse>(string endpoint, TRequest payload)
        {
            string jsonPayload = JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions);
            using StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await SendHttpRequestAsync(() => _internalHttpClient.PatchAsync(endpoint, content), endpoint);
            await EnsureSuccessStatusCodeAsync(response);
            return await DeserializeResponseAsync<TResponse>(response);
        }

        private async Task SendPatchRequestAsync<TRequest>(string endpoint, TRequest payload)
        {
            string jsonPayload = JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions);
            using StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await SendHttpRequestAsync(() => _internalHttpClient.PatchAsync(endpoint, content), endpoint);
            await EnsureSuccessStatusCodeAsync(response);
            response.Content?.Dispose();
        }

        private async Task SendDeleteRequestAsync(string endpoint)
        {
            HttpResponseMessage response = await SendHttpRequestAsync(() => _internalHttpClient.DeleteAsync(endpoint), endpoint);
            await EnsureSuccessStatusCodeAsync(response);
            response.Content?.Dispose();
        }

        private async Task<TResponse> SendDeleteRequestWithResponseAsync<TResponse>(string endpoint)
        {
            var responseMessage = await SendHttpRequestAsync(async () =>
            {
                var freshRequest = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                return await _internalHttpClient.SendAsync(freshRequest, HttpCompletionOption.ResponseHeadersRead);
            }, endpoint);

            await EnsureSuccessStatusCodeAsync(responseMessage);

            if (responseMessage.StatusCode == HttpStatusCode.NoContent)
            {
                responseMessage.Content?.Dispose();
                return default(TResponse);
            }
            return await DeserializeResponseAsync<TResponse>(responseMessage);
        }

        private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                response.Content?.Dispose();
                throw new HttpRequestException($"Request failed with status code {response.StatusCode}: {errorContent}", null, response.StatusCode);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_isExternalClient)
                {
                    _internalHttpClient?.Dispose();
                }
                _rateLimitSemaphore?.Dispose();
            }
        }
    }
}
