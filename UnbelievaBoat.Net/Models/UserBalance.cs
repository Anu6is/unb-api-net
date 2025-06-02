using System.Text.Json.Serialization;

namespace UnbelievaBoat.Net.Models
{
    public record UserBalance
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; init; }

        [JsonPropertyName("guild_id")]
        public string GuildId { get; init; }

        [JsonPropertyName("cash")]
        public long Cash { get; init; }

        [JsonPropertyName("bank")]
        public long Bank { get; init; }

        [JsonPropertyName("total")]
        public long Total { get; init; }

        [JsonPropertyName("rank")]
        public int? Rank { get; init; } // Rank can sometimes be null
    }
}
