using System.Text.Json.Serialization;

namespace UnbelievaBoat.Net.Models
{
    public record GuildRole
    {
        [JsonPropertyName("id")]
        public string Id { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; }

        [JsonPropertyName("permissions")]
        public long Permissions { get; init; } // Permissions are often represented as a bitfield long

        [JsonPropertyName("position")]
        public int Position { get; init; }

        [JsonPropertyName("is_managed")]
        public bool IsManaged { get; init; }

        [JsonPropertyName("is_hoisted")]
        public bool IsHoisted { get; init; }

        [JsonPropertyName("is_mentionable")]
        public bool IsMentionable { get; init; }

        [JsonPropertyName("color")]
        public int Color { get; init; }
    }
}
