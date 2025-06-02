# UnbelievaBoat.Net API Client

**UnbelievaBoat.Net** is a C# .NET 8 client library for interacting with the [UnbelievaBoat API](https://unbelievaboat-api.readme.io/reference/reference). It provides convenient access to all API endpoints, handles rate limiting, and includes configurable retry mechanisms.

## Features

*   Targets .NET 8
*   Full coverage of UnbelievaBoat API endpoints:
    *   Economy (User Balances, Guild Leaderboards)
    *   Guild Information
    *   Application Permissions
    *   Store Item Management
    *   User Inventory Management
*   Automatic rate limit handling based on API headers.
*   Configurable retry policies for transient errors (using Polly).
*   Strongly-typed request and response models.
*   Asynchronous operations using `async`/`await`.
*   Explicit error handling using the `Result<TSuccess, TError>` pattern.

## Installation

*(Once the package is published to NuGet.org)*

You can install the package via NuGet Package Manager Console:
```powershell
Install-Package UnbelievaBoat.Net
```
Or via .NET CLI:
```bash
dotnet add package UnbelievaBoat.Net
```

## Usage

### 1. Initialize the Client

First, you need to instantiate the `UnbelievaBoatClient`. You'll need your UnbelievaBoat API token.

```csharp
using UnbelievaBoat.Net;
using System;
using System.Threading.Tasks;

// ...

string apiToken = "YOUR_UNBELIEVABOAT_API_TOKEN";
var client = new UnbelievaBoatClient(apiToken);

// You can also specify a custom base URL if needed (though default is provided)
// var client = new UnbelievaBoatClient(apiToken, "https://unbelievaboat.com/api/v1");
```

### 2. Making API Calls

All API calls are asynchronous and return a `Task<Result<TSuccess, ApiError>>`.

**Example: Get User Balance**
```csharp
using UnbelievaBoat.Net.Models; // For UserBalance DTO
using UnbelievaBoat.Net.Common; // For Result and ApiError
using System;
using System.Threading.Tasks;

// ... (client initialization) ...

async Task GetBalanceExample(string guildId, string userId)
{
    Result<UserBalance, ApiError> result = await client.GetUserBalanceAsync(guildId, userId);

    if (result.IsSuccess)
    {
        UserBalance balance = result.Value;
        Console.WriteLine($"User {userId} in guild {guildId}:");
        Console.WriteLine($"  Cash: {balance.Cash}");
        Console.WriteLine($"  Bank: {balance.Bank}");
        Console.WriteLine($"  Total: {balance.Total}");
        if (balance.Rank.HasValue)
        {
            Console.WriteLine($"  Rank: {balance.Rank.Value}");
        }
    }
    else
    {
        ApiError error = result.Error;
        Console.WriteLine($"API Error Occurred:");
        Console.WriteLine($"  Status Code: {error.StatusCode}");
        Console.WriteLine($"  Message: {error.Message}");
        // You can also inspect error.RawContent or error.Headers if needed
        // Console.WriteLine($"  Raw Content: {error.RawContent}");
    }
}
```

**Example: Get Guild Leaderboard**
```csharp
using UnbelievaBoat.Net.Models;
using UnbelievaBoat.Net.Common;
using System;
using System.Threading.Tasks;

// ... (client initialization) ...

async Task GetLeaderboardExample(string guildId)
{
    var options = new UnbelievaBoatClient.GuildLeaderboardOptions
    {
        Limit = 10, // Get top 10 users
        Sort = "-total" // Sort by total balance, descending
    };

    Result<GuildLeaderboard, ApiError> result = await client.GetGuildLeaderboardAsync(guildId, options);

    if (result.IsSuccess)
    {
        GuildLeaderboard leaderboard = result.Value;
        Console.WriteLine($"Top {options.Limit} users in guild {leaderboard.GuildId} (by total balance):");
        foreach (var userBalance in leaderboard.Users)
        {
            Console.WriteLine($"  User: {userBalance.UserId}, Total: {userBalance.Total}, Rank: {userBalance.Rank?.ToString() ?? "N/A"}");
        }
    }
    else
    {
        ApiError error = result.Error;
        Console.WriteLine($"API Error: {error.StatusCode} - {error.Message}");
    }
}
```

**Example: Add an Item to a User's Inventory**
```csharp
using UnbelievaBoat.Net.Models; // For AddInventoryItemRequest & InventoryItem
using UnbelievaBoat.Net.Common;
using System;
using System.Threading.Tasks;

// ... (client initialization) ...

async Task AddItemToInventoryExample(string guildId, string userId, string storeItemId, int quantity)
{
    var payload = new AddInventoryItemRequest
    {
        Quantity = quantity
        // ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) // Optional: set expiration
    };

    Result<InventoryItem, ApiError> result = await client.AddUserInventoryItemAsync(guildId, userId, storeItemId, payload);

    if (result.IsSuccess)
    {
        InventoryItem updatedInventoryItem = result.Value;
        Console.WriteLine($"Successfully added {updatedInventoryItem.Quantity} of item '{updatedInventoryItem.Name}' (ID: {updatedInventoryItem.ItemId}) to user {userId}.");
        Console.WriteLine($"  New inventory instance ID: {updatedInventoryItem.Id}");
    }
    else
    {
        ApiError error = result.Error;
        Console.WriteLine($"API Error: {error.StatusCode} - {error.Message}");
    }
}
```

### 3. Configuring Retry Policy

The client includes a configurable retry policy for transient network errors and specific HTTP status codes (5xx, and optionally 429). By default, it retries 3 times with exponential backoff. This retry logic occurs before a `Result` is formed.

You can customize this by passing a `RetryPolicyConfig` to the client's constructor:

```csharp
var retryConfig = new RetryPolicyConfig
{
    MaxRetries = 5, // Number of retry attempts
    InitialDelay = TimeSpan.FromSeconds(1), // Delay for the first retry
    MaxDelay = TimeSpan.FromSeconds(30), // Maximum delay between retries
    BackoffFactor = 2.0, // Factor by which delay increases (e.g., 1s, 2s, 4s, ...)
    RetryOnRateLimitExceeded = true // Set to true to also retry on 429 (Too Many Requests)
};

var clientWithCustomRetries = new UnbelievaBoatClient(apiToken, retryConfig: retryConfig);
```
If you want to disable retries:
```csharp
var noRetryConfig = new RetryPolicyConfig { MaxRetries = 0 };
var clientWithoutRetries = new UnbelievaBoatClient(apiToken, retryConfig: noRetryConfig);
```

### 4. Rate Limiting

The client automatically respects API rate limits reported via `X-RateLimit-Remaining` and `X-RateLimit-Reset-After` headers. If a request would exceed the limit, the client will pause asynchronously until the limit resets.

You can inspect the last known rate limit values from the client:
```csharp
if (client.LastRateLimitLimit.HasValue)
{
    Console.WriteLine($"Last API Rate Limit: {client.LastRateLimitLimit.Value} requests.");
}
if (client.LastRateLimitRemaining.HasValue)
{
    Console.WriteLine($"Remaining Requests: {client.LastRateLimitRemaining.Value}.");
}
if (client.LastRateLimitResetTimeUtc.HasValue)
{
    Console.WriteLine($"Resets at (UTC): {client.LastRateLimitResetTimeUtc.Value:O}");
}
```

### 5. Error Handling

The **UnbelievaBoat.Net** client uses a `Result<TSuccess, TError>` pattern to handle outcomes of API calls. This means that instead of throwing exceptions for API operational errors (like 4xx or 5xx status codes), methods return a `Result` object.

The `Result<TSuccess, TError>` object has an `IsSuccess` property:
*   If `IsSuccess` is `true`, the API call was successful, and you can access the expected data from the `Value` property (e.g., `result.Value`).
*   If `IsSuccess` is `false`, an error occurred during the API call. Details about the error can be accessed from the `Error` property (e.g., `result.Error`).

The `Error` object is typically an `ApiError` (found in `UnbelievaBoat.Net.Common`), which contains:
*   `StatusCode`: The `System.Net.HttpStatusCode` returned by the API.
*   `Message`: A descriptive error message. This often includes details from the API's error response.
*   `RawContent`: The raw JSON (or other) string content of the error response from the API.
*   `Headers`: A read-only dictionary of the HTTP response headers associated with the error.

**Example of checking a result:**
```csharp
// Assuming 'client' is an initialized UnbelievaBoatClient
// and SomeModel is an expected response type from UnbelievaBoat.Net.Models
// and ApiError is from UnbelievaBoat.Net.Common
// Result<SomeModel, ApiError> apiResult = await client.SomeMethodAsync(); // Replace SomeMethodAsync with an actual client method

// if (apiResult.IsSuccess)
// {
//     SomeModel data = apiResult.Value;
//     // Process successful data
// }
// else
// {
//     ApiError errorDetails = apiResult.Error;
//     Console.WriteLine($"API Error: {errorDetails.StatusCode} - {errorDetails.Message}");
//     // Log errorDetails.RawContent or inspect errorDetails.Headers for more context
// }
```
*(Note: Replace `SomeMethodAsync` and `SomeModel` with actual client methods and response types in your code.)*

While API operational errors are handled via the `Result` pattern, `HttpRequestException` might still be thrown for lower-level network issues (e.g., DNS failures, connection timeouts before a response is received) that occur before an HTTP response can be processed, especially if these issues are not caught or retried by the configured Polly policies. Client-side argument validation errors (e.g., passing `null` for a required ID) will still throw exceptions like `ArgumentNullException` or `ArgumentException` before any API call is attempted.

### 6. Disposing the Client

The `UnbelievaBoatClient` implements `IDisposable` and should be disposed of when no longer needed, especially if you are not providing an external `HttpClient` instance. This ensures the underlying `HttpClient` is cleaned up properly.

```csharp
// Using statement ensures disposal
using (var client = new UnbelievaBoatClient(apiToken))
{
    // Use the client
}
```

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

This library is released under the MIT License.
```
