using System.Text.Json.Serialization;

namespace UnbelievaBoat.Net.Models
{
    public record RemoveInventoryItemRequest
    {
        // DELETE /users/{user_id}/guilds/{guild_id}/inventory/{item_id}
        // Typically DELETE requests might not have a body, or if they do, it's for specific conditions.
        // The API docs for "Remove Inventory Item" show a query parameter `quantity`.
        // If a body were supported for bulk/conditional removal, it would be defined here.
        // For now, let's assume quantity is via query param and body is not used for DELETE.
        // If the API expects a body for DELETE (e.g. specifying quantity to remove), this would be the place.
        // The docs show query params: `quantity` (integer), `inventory_item_id` (string).
        // `inventory_item_id` is confusing if `item_id` (store item id) is already in path.
        // This implies you might be deleting specific instances of an item if a user can have multiple distinct stacks.
        // Let's assume `item_id` in path is the *Store Item ID* and `inventory_item_id` in query is the *specific instance ID in inventory*.
        // The path for DELETE is /users/{user_id}/guilds/{guild_id}/inventory/{item_id}
        // This is ambiguous. Let's assume item_id in path is the store item's ID.
        // And the query param `quantity` is how many to remove.
        // No request body DTO needed if only query params are used for DELETE.
        // For consistency with Add, if quantity could be in body:
        [JsonPropertyName("quantity")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Quantity { get; set; }
    }
}
