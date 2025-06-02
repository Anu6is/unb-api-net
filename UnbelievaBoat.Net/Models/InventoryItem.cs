using System.Text.Json.Serialization;
using System;

namespace UnbelievaBoat.Net.Models
{
    public record InventoryItem
    {
        [JsonPropertyName("id")]
        public string Id { get; init; }

        [JsonPropertyName("item_id")]
        public string ItemId { get; init; } // Refers to StoreItem ID

        [JsonPropertyName("name")]
        public string Name { get; init; }

        [JsonPropertyName("description")]
        public string Description { get; init; }

        // Price might not be relevant for an inventory item itself, but was in docs.
        // It might be the price at which it was bought.
        [JsonPropertyName("price")]
        public long? Price { get; init; }

        [JsonPropertyName("hidden")]
        public bool? Hidden { get; init; } // Item properties at time of grant

        [JsonPropertyName("required_role_id")]
        public string RequiredRoleId { get; init; }

        [JsonPropertyName("max_user_quantity")]
        public int? MaxUserQuantity { get; init; }

        [JsonPropertyName("reply_message")]
        public string ReplyMessage { get; init; }

        [JsonPropertyName("priority")]
        public int? Priority { get; init; }

        [JsonPropertyName("user_id")]
        public string UserId { get; init; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; init; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; } // Settable for add/remove operations

        [JsonPropertyName("created_at")]
        public DateTimeOffset? CreatedAt { get; init; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
