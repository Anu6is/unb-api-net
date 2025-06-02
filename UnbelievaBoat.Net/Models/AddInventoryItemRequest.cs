using System.Text.Json.Serialization;
using System;

namespace UnbelievaBoat.Net.Models
{
    public record AddInventoryItemRequest
    {
        // For POST /users/{userId}/guilds/{guildId}/inventory/{itemId}
        // The body is actually empty for adding, quantity is a query param.
        // For POST /users/{userId}/guilds/{guildId}/inventory (if it were to create a new custom item in inventory)
        // it might need more. The current POST is to add quantity of an existing store item.
        // The documentation for "Add Inventory Item" POST /users/{user_id}/guilds/{guild_id}/inventory/{item_id}
        // implies an empty body or specific fields if it were creating an item.
        // However, the linked "Inventory Item Structure" is a response structure.
        // Let's assume for now the Add operation on an existing item ID might take quantity in body for clarity,
        // or it will be a query parameter as often seen.
        // The docs for POST /users/{user_id}/guilds/{guild_id}/inventory/{item_id} state:
        // "Request body (application/json) - The new inventory item object." which is confusing if item_id is in path.
        // Let's assume it's for overriding properties or quantity.
        // The reference for "Add Inventory Item" (POST /users/{user_id}/guilds/{guild_id}/inventory/{item_id})
        // has a request body example: { "quantity": 0, "expires_at": "2024-06-15T10:00:00.000Z" }
        // This means the body is for quantity and potentially other instance-specific details.

        [JsonPropertyName("quantity")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Quantity { get; set; }

        [JsonPropertyName("expires_at")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? ExpiresAt { get; set; }
    }
}
