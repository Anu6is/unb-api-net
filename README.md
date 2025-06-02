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

All API calls are asynchronous and return `Task` or `Task<T>`.

**Example: Get User Balance**
```csharp
using UnbelievaBoat.Net.Models; // For UserBalance DTO

async Task GetBalanceExample(string guildId, string userId)
{
    try
    {
        UserBalance balance = await client.GetUserBalanceAsync(guildId, userId);
        Console.WriteLine($"User {userId} in guild {guildId}:");
        Console.WriteLine($"  Cash: {balance.Cash}");
        Console.WriteLine($"  Bank: {balance.Bank}");
        Console.WriteLine($"  Total: {balance.Total}");
        if (balance.Rank.HasValue)
        {
            Console.WriteLine($"  Rank: {balance.Rank.Value}");
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"API Error: {ex.Message}");
        // ex.StatusCode might be available if using a custom exception that wraps it,
        // or parse from message if needed. Default HttpRequestException has StatusCode property in .NET 5+
        if (ex.StatusCode.HasValue)
        {
            Console.WriteLine($"Status Code: {ex.StatusCode.Value}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An unexpected error occurred: {ex.Message}");
    }
}
```

**Example: Get Guild Leaderboard**
```csharp
async Task GetLeaderboardExample(string guildId)
{
    var options = new UnbelievaBoatClient.GuildLeaderboardOptions
    {
        Limit = 10, // Get top 10 users
        Sort = "-total" // Sort by total balance, descending
    };

    try
    {
        GuildLeaderboard leaderboard = await client.GetGuildLeaderboardAsync(guildId, options);
        Console.WriteLine($"Top {options.Limit} users in guild {leaderboard.GuildId} (by total balance):");
        foreach (var userBalance in leaderboard.Users)
        {
            Console.WriteLine($"  User: {userBalance.UserId}, Total: {userBalance.Total}, Rank: {userBalance.Rank?.ToString() ?? "N/A"}");
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"API Error: {ex.Message}");
    }
}
```

**Example: Add an Item to a User's Inventory**
```csharp
using UnbelievaBoat.Net.Models; // For AddInventoryItemRequest

async Task AddItemToInventoryExample(string guildId, string userId, string storeItemId, int quantity)
{
    var payload = new AddInventoryItemRequest
    {
        Quantity = quantity
        // ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) // Optional: set expiration
    };

    try
    {
        InventoryItem updatedInventoryItem = await client.AddUserInventoryItemAsync(guildId, userId, storeItemId, payload);
        Console.WriteLine($"Successfully added {updatedInventoryItem.Quantity} of item '{updatedInventoryItem.Name}' (ID: {updatedInventoryItem.ItemId}) to user {userId}.");
        Console.WriteLine($"  New inventory instance ID: {updatedInventoryItem.Id}");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"API Error: {ex.Message}");
    }
}
```

### 3. Configuring Retry Policy

The client includes a configurable retry policy for transient network errors and specific HTTP status codes (5xx, and optionally 429). By default, it retries 3 times with exponential backoff.

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

API errors (non-2xx status codes) will result in an `HttpRequestException`. The exception message typically includes the status code and the error content returned by the API, which can be useful for debugging.

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
