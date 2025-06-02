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
using UnbelievaBoat.Net.Common; // Added for Result and ApiError
using System.Text.Json.Serialization;

namespace UnbelievaBoat.Net
{
    public class UnbelievaBoatClient : IDisposable
    {
        private readonly HttpClient _internalHttpClient;
        private readonly bool _isExternalClient;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
        private readonly string _apiToken;
        private readonly string _baseUrl;

        private const string DefaultApiBaseUrl = "https://unbelievaboat.com/api/v1";

        public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Inner class for GuildLeaderboardOptions
        public class GuildLeaderboardOptions
        {
            public int? Limit { get; set; }
            public int? Offset { get; set; }
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
            public string Sort { get; set; }
            public bool? Hidden { get; set; }
        }

        // Inner class for UserInventoryQueryOptions
        public class UserInventoryQueryOptions
        {
            public int? Limit { get; set; }
            public int? Offset { get; set; }
            public string Sort { get; set; }
            public string StoreItemId { get; set; }
        }

        public class RemoveUserInventoryItemOptions
        {
            public int? Quantity { get; set; }
            public string InventoryInstanceId { get; set; }
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
            await _rateLimitSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_lastRateLimitRemaining.HasValue && _lastRateLimitRemaining <= 1 && _rateLimitResetTimeUtc.HasValue && _rateLimitResetTimeUtc > DateTime.UtcNow)
                {
                    var delay = _rateLimitResetTimeUtc.Value - DateTime.UtcNow;
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay).ConfigureAwait(false);
                    }
                }

                httpResponseMessage = await _retryPolicy.ExecuteAsync(requestFactory).ConfigureAwait(false);

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

        // Public API methods will change to return Result<T, ApiError>
        // Example:
        // public async Task<Result<UserBalance, ApiError>> GetUserBalanceAsync(string guildId, string userId)
        // {
        //     // ... validation ...
        //     var endpoint = $"/guilds/{guildId}/users/{userId}";
        //     return await SendGetRequestAsync<UserBalance>(endpoint);
        // }
        // This pattern will be applied to all public methods.

        // Balance Endpoints
        public async Task<Result<UserBalance, ApiError>> GetUserBalanceAsync(string guildId, string userId)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));

            var endpoint = $"/guilds/{guildId}/users/{userId}";
            return await SendGetRequestAsync<UserBalance>(endpoint).ConfigureAwait(false);
        }

        public async Task<Result<UserBalance, ApiError>> SetUserBalanceAsync(string guildId, string userId, UpdateUserBalanceRequest payload)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            var endpoint = $"/guilds/{guildId}/users/{userId}";
            return await SendPutRequestAsync<UpdateUserBalanceRequest, UserBalance>(endpoint, payload).ConfigureAwait(false);
        }

        public async Task<Result<UserBalance, ApiError>> UpdateUserBalanceAsync(string guildId, string userId, UpdateUserBalanceRequest payload)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            var endpoint = $"/guilds/{guildId}/users/{userId}";
            return await SendPatchRequestAsync<UpdateUserBalanceRequest, UserBalance>(endpoint, payload).ConfigureAwait(false);
        }

        public async Task<Result<GuildLeaderboard, ApiError>> GetGuildLeaderboardAsync(string guildId, GuildLeaderboardOptions options = null)
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
                    var queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                    endpoint += $"?{queryString}";
                }
            }
            return await SendGetRequestAsync<GuildLeaderboard>(endpoint).ConfigureAwait(false);
        }

        // Guild Endpoints
        public async Task<Result<Guild, ApiError>> GetGuildAsync(string guildId)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            var endpoint = $"/guilds/{guildId}";
            return await SendGetRequestAsync<Guild>(endpoint).ConfigureAwait(false);
        }

        // Application Endpoints
        public async Task<Result<ApplicationPermissions, ApiError>> GetApplicationPermissionsAsync()
        {
            var endpoint = "/applications/@me/permissions";
            return await SendGetRequestAsync<ApplicationPermissions>(endpoint).ConfigureAwait(false);
        }

        // Store Item Endpoints
        public async Task<Result<List<StoreItem>, ApiError>> GetStoreItemsAsync(string guildId, StoreItemsQueryOptions options = null)
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
                if (queryParams.Any()) endpoint += $"?{string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"))}";
            }
            return await SendGetRequestAsync<List<StoreItem>>(endpoint).ConfigureAwait(false);
        }

        public async Task<Result<StoreItem, ApiError>> GetStoreItemAsync(string guildId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item ID cannot be null or whitespace.", nameof(itemId));
            var endpoint = $"/guilds/{guildId}/items/{itemId}";
            return await SendGetRequestAsync<StoreItem>(endpoint).ConfigureAwait(false);
        }

        public async Task<Result<StoreItem, ApiError>> CreateStoreItemAsync(string guildId, StoreItem itemToCreate)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (itemToCreate == null) throw new ArgumentNullException(nameof(itemToCreate));
            var endpoint = $"/guilds/{guildId}/items";
            return await SendPostRequestAsync<StoreItem, StoreItem>(endpoint, itemToCreate).ConfigureAwait(false);
        }

        public async Task<Result<StoreItem, ApiError>> EditStoreItemAsync(string guildId, string itemId, StoreItem itemUpdates)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item ID cannot be null or whitespace.", nameof(itemId));
            if (itemUpdates == null) throw new ArgumentNullException(nameof(itemUpdates));
            var endpoint = $"/guilds/{guildId}/items/{itemId}";
            return await SendPatchRequestAsync<StoreItem, StoreItem>(endpoint, itemUpdates).ConfigureAwait(false);
        }

        public async Task<Result<OkStatus, ApiError>> DeleteStoreItemAsync(string guildId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item ID cannot be null or whitespace.", nameof(itemId));
            var endpoint = $"/guilds/{guildId}/items/{itemId}";
            return await SendDeleteRequestAsync(endpoint).ConfigureAwait(false);
        }

        // User Inventory Endpoints
        public async Task<Result<List<InventoryItem>, ApiError>> GetUserInventoryItemsAsync(string guildId, string userId, UserInventoryQueryOptions options = null)
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
                if (queryParams.Any()) endpoint += $"?{string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"))}";
            }
            return await SendGetRequestAsync<List<InventoryItem>>(endpoint).ConfigureAwait(false);
        }

        public async Task<Result<InventoryItem, ApiError>> GetUserInventoryItemAsync(string guildId, string userId, string inventoryItemId)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));
            if (string.IsNullOrWhiteSpace(inventoryItemId)) throw new ArgumentException("Inventory Item ID cannot be null or whitespace.", nameof(inventoryItemId));
            var endpoint = $"/users/{userId}/guilds/{guildId}/inventory/{inventoryItemId}";
            return await SendGetRequestAsync<InventoryItem>(endpoint).ConfigureAwait(false);
        }

        public async Task<Result<InventoryItem, ApiError>> AddUserInventoryItemAsync(string guildId, string userId, string storeItemId, AddInventoryItemRequest payload)
        {
            if (string.IsNullOrWhiteSpace(guildId)) throw new ArgumentException("Guild ID cannot be null or whitespace.", nameof(guildId));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));
            if (string.IsNullOrWhiteSpace(storeItemId)) throw new ArgumentException("Store Item ID cannot be null or whitespace.", nameof(storeItemId));
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (payload.Quantity.HasValue && payload.Quantity.Value <= 0) throw new ArgumentOutOfRangeException(nameof(payload.Quantity), "Quantity to add must be positive.");
            var endpoint = $"/users/{userId}/guilds/{guildId}/inventory/{storeItemId}";
            return await SendPostRequestAsync<AddInventoryItemRequest, InventoryItem>(endpoint, payload).ConfigureAwait(false);
        }

        public async Task<Result<InventoryItem, ApiError>> RemoveUserInventoryItemAsync(string guildId, string userId, string storeItemId, RemoveUserInventoryItemOptions options = null)
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
                if (!string.IsNullOrWhiteSpace(options.InventoryInstanceId)) queryParams["inventory_item_id"] = options.InventoryInstanceId;
            }
            if (queryParams.Any()) endpoint += $"?{string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"))}";
            return await SendDeleteRequestWithResponseAsync<InventoryItem>(endpoint).ConfigureAwait(false);
        }

        // Generic Send Methods
        private async Task<ApiError> TryParseApiErrorAsync(HttpResponseMessage responseMessage)
        {
            if (responseMessage.IsSuccessStatusCode)
            {
                return null;
            }

            string errorContent = null;
            if (responseMessage.Content != null)
            {
                errorContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            var headersDictionary = responseMessage.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            if (responseMessage.Content?.Headers != null)
            {
                foreach (var header in responseMessage.Content.Headers)
                {
                    if (!headersDictionary.ContainsKey(header.Key))
                    {
                        headersDictionary[header.Key] = header.Value;
                    }
                }
            }

            return new ApiError(
                responseMessage.StatusCode,
                $"API request failed: {responseMessage.ReasonPhrase} (Status: {responseMessage.StatusCode}). Raw content: {errorContent?.Substring(0, Math.Min(errorContent?.Length ?? 0, 500))}",
                errorContent,
                headersDictionary
            );
        }

        private async Task<Result<TResponse, ApiError>> DeserializeResponseAsync<TResponse>(HttpResponseMessage responseMessage)
        {
            try
            {
                if (responseMessage.Content == null)
                {
                    if (typeof(TResponse) == typeof(string))
                    {
                        responseMessage.Content?.Dispose();
                        return Result<TResponse, ApiError>.Success((TResponse)(object)string.Empty);
                    }
                    var headers = responseMessage.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    return Result<TResponse, ApiError>.Failure(new ApiError(responseMessage.StatusCode, "Response content was null or empty when a body was expected.", null, headers));
                }

                if (responseMessage.StatusCode == HttpStatusCode.NoContent)
                {
                    responseMessage.Content.Dispose();
                    if (typeof(TResponse) == typeof(OkStatus))
                    {
                        return Result<TResponse, ApiError>.Success((TResponse)(object)OkStatus.Instance);
                    }
                    return Result<TResponse, ApiError>.Success(default(TResponse));
                }

                var responseStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
                if (responseStream == null || responseStream.Length == 0)
                {
                     if (typeof(TResponse) == typeof(string))
                     {
                        responseMessage.Content.Dispose();
                        return Result<TResponse, ApiError>.Success((TResponse)(object)string.Empty);
                     }
                     var headers = responseMessage.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                     responseMessage.Content.Dispose();
                     return Result<TResponse, ApiError>.Failure(new ApiError(responseMessage.StatusCode, "Response content stream was null or empty when a body was expected.", null, headers));
                }

                var result = await JsonSerializer.DeserializeAsync<TResponse>(responseStream, DefaultJsonSerializerOptions).ConfigureAwait(false);
                return Result<TResponse, ApiError>.Success(result);
            }
            catch (JsonException jsonEx)
            {
                string rawContent = null;
                try { rawContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false); } catch { /* ignore */ }
                var headers = responseMessage.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return Result<TResponse, ApiError>.Failure(new ApiError(
                    responseMessage.StatusCode,
                    $"Failed to deserialize response: {jsonEx.Message}. Path: {jsonEx.Path}, Line: {jsonEx.LineNumber}, Pos: {jsonEx.BytePositionInLine}",
                    rawContent,
                    headers
                ));
            }
            catch (Exception ex)
            {
                string rawContent = null;
                try { rawContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false); } catch { /* ignore */ }
                var headers = responseMessage.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return Result<TResponse, ApiError>.Failure(new ApiError(
                    responseMessage.StatusCode,
                    $"An unexpected error occurred during deserialization: {ex.Message}",
                    rawContent,
                    headers
                ));
            }
            finally
            {
                responseMessage.Content?.Dispose();
            }
        }

        internal async Task<Result<TResponse, ApiError>> SendGetRequestAsync<TResponse>(string endpoint)
        {
            var responseMessage = await SendHttpRequestAsync(() =>
            {
                var freshRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
                return _internalHttpClient.SendAsync(freshRequest, HttpCompletionOption.ResponseHeadersRead);
            }, endpoint).ConfigureAwait(false);

            var apiError = await TryParseApiErrorAsync(responseMessage).ConfigureAwait(false);
            if (apiError != null)
            {
                responseMessage.Content?.Dispose();
                return Result<TResponse, ApiError>.Failure(apiError);
            }
            return await DeserializeResponseAsync<TResponse>(responseMessage).ConfigureAwait(false);
        }

        internal async Task<Result<TResponse, ApiError>> SendPostRequestAsync<TRequest, TResponse>(string endpoint, TRequest payload)
        {
            var jsonPayload = JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions);
            var responseMessage = await SendHttpRequestAsync(() =>
            {
                var freshRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };
                return _internalHttpClient.SendAsync(freshRequest, HttpCompletionOption.ResponseHeadersRead);
            }, endpoint).ConfigureAwait(false);

            var apiError = await TryParseApiErrorAsync(responseMessage).ConfigureAwait(false);
            if (apiError != null)
            {
                responseMessage.Content?.Dispose();
                return Result<TResponse, ApiError>.Failure(apiError);
            }
            return await DeserializeResponseAsync<TResponse>(responseMessage).ConfigureAwait(false);
        }

        internal async Task<Result<OkStatus, ApiError>> SendPostRequestAsync<TRequest>(string endpoint, TRequest payload)
        {
            var jsonPayload = JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions);
            var responseMessage = await SendHttpRequestAsync(() =>
            {
                var freshRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };
                return _internalHttpClient.SendAsync(freshRequest, HttpCompletionOption.ResponseHeadersRead);
            }, endpoint).ConfigureAwait(false);

            var apiError = await TryParseApiErrorAsync(responseMessage).ConfigureAwait(false);
            responseMessage.Content?.Dispose(); // Always dispose content for OkStatus results
            if (apiError != null)
            {
                return Result<OkStatus, ApiError>.Failure(apiError);
            }
            return Result<OkStatus, ApiError>.Success(OkStatus.Instance);
        }

        internal async Task<Result<TResponse, ApiError>> SendPutRequestAsync<TRequest, TResponse>(string endpoint, TRequest payload)
        {
            var jsonPayload = JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions);
            var responseMessage = await SendHttpRequestAsync(() =>
            {
                var freshRequest = new HttpRequestMessage(HttpMethod.Put, endpoint)
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };
                return _internalHttpClient.SendAsync(freshRequest, HttpCompletionOption.ResponseHeadersRead);
            }, endpoint).ConfigureAwait(false);

            var apiError = await TryParseApiErrorAsync(responseMessage).ConfigureAwait(false);
            if (apiError != null)
            {
                responseMessage.Content?.Dispose();
                return Result<TResponse, ApiError>.Failure(apiError);
            }
            return await DeserializeResponseAsync<TResponse>(responseMessage).ConfigureAwait(false);
        }

        internal async Task<Result<OkStatus, ApiError>> SendPutRequestAsync<TRequest>(string endpoint, TRequest payload)
        {
            var jsonPayload = JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions);
            var responseMessage = await SendHttpRequestAsync(() =>
            {
                var freshRequest = new HttpRequestMessage(HttpMethod.Put, endpoint)
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };
                return _internalHttpClient.SendAsync(freshRequest, HttpCompletionOption.ResponseHeadersRead);
            }, endpoint).ConfigureAwait(false);

            var apiError = await TryParseApiErrorAsync(responseMessage).ConfigureAwait(false);
            responseMessage.Content?.Dispose();
            if (apiError != null)
            {
                return Result<OkStatus, ApiError>.Failure(apiError);
            }
            return Result<OkStatus, ApiError>.Success(OkStatus.Instance);
        }

        internal async Task<Result<TResponse, ApiError>> SendPatchRequestAsync<TRequest, TResponse>(string endpoint, TRequest payload)
        {
            var jsonPayload = JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions);
            var responseMessage = await SendHttpRequestAsync(() =>
            {
                var freshRequest = new HttpRequestMessage(new HttpMethod("PATCH"), endpoint)
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };
                return _internalHttpClient.SendAsync(freshRequest, HttpCompletionOption.ResponseHeadersRead);
            }, endpoint).ConfigureAwait(false);

            var apiError = await TryParseApiErrorAsync(responseMessage).ConfigureAwait(false);
            if (apiError != null)
            {
                responseMessage.Content?.Dispose();
                return Result<TResponse, ApiError>.Failure(apiError);
            }
            return await DeserializeResponseAsync<TResponse>(responseMessage).ConfigureAwait(false);
        }

        internal async Task<Result<OkStatus, ApiError>> SendPatchRequestAsync<TRequest>(string endpoint, TRequest payload)
        {
            var jsonPayload = JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions);
            var responseMessage = await SendHttpRequestAsync(() =>
            {
                var freshRequest = new HttpRequestMessage(new HttpMethod("PATCH"), endpoint)
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };
                return _internalHttpClient.SendAsync(freshRequest, HttpCompletionOption.ResponseHeadersRead);
            }, endpoint).ConfigureAwait(false);

            var apiError = await TryParseApiErrorAsync(responseMessage).ConfigureAwait(false);
            responseMessage.Content?.Dispose();
            if (apiError != null)
            {
                return Result<OkStatus, ApiError>.Failure(apiError);
            }
            return Result<OkStatus, ApiError>.Success(OkStatus.Instance);
        }

        internal async Task<Result<OkStatus, ApiError>> SendDeleteRequestAsync(string endpoint)
        {
            var responseMessage = await SendHttpRequestAsync(() =>
            {
                var freshRequest = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                return _internalHttpClient.SendAsync(freshRequest, HttpCompletionOption.ResponseHeadersRead);
            }, endpoint).ConfigureAwait(false);

            var apiError = await TryParseApiErrorAsync(responseMessage).ConfigureAwait(false);
            responseMessage.Content?.Dispose();
            if (apiError != null)
            {
                return Result<OkStatus, ApiError>.Failure(apiError);
            }
            return Result<OkStatus, ApiError>.Success(OkStatus.Instance);
        }

        internal async Task<Result<TResponse, ApiError>> SendDeleteRequestWithResponseAsync<TResponse>(string endpoint)
        {
            var responseMessage = await SendHttpRequestAsync(async () =>
            {
                var freshRequest = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                return await _internalHttpClient.SendAsync(freshRequest, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            }, endpoint).ConfigureAwait(false);

            var apiError = await TryParseApiErrorAsync(responseMessage).ConfigureAwait(false);
            if (apiError != null)
            {
                responseMessage.Content?.Dispose();
                return Result<TResponse, ApiError>.Failure(apiError);
            }
            return await DeserializeResponseAsync<TResponse>(responseMessage).ConfigureAwait(false);
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
