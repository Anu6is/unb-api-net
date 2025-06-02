using System.Text.Json.Serialization;
using System;

namespace UnbelievaBoat.Net.Models
{
    public record StoreItem
    {
        [JsonPropertyName("id")]
        public string Id { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; set; } // Settable for create/edit

        [JsonPropertyName("description")]
        public string Description { get; set; } // Settable for create/edit

        [JsonPropertyName("price")]
        public long? Price { get; set; } // Nullable for PATCH

        [JsonPropertyName("stock_remaining")]
        public int? StockRemaining { get; init; } // Usually read-only from client perspective

        [JsonPropertyName("max_stock")]
        public int? MaxStock { get; set; }

        [JsonPropertyName("hidden")]
        public bool? Hidden { get; set; }

        [JsonPropertyName("required_role_id")]
        public string RequiredRoleId { get; set; }

        [JsonPropertyName("max_user_quantity")]
        public int? MaxUserQuantity { get; set; }

        [JsonPropertyName("reply_message")]
        public string ReplyMessage { get; set; }

        [JsonPropertyName("priority")]
        public int? Priority { get; set; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset? CreatedAt { get; init; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
