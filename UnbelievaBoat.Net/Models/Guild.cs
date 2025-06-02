using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnbelievaBoat.Net.Models
{
    public record Guild
    {
        [JsonPropertyName("id")]
        public string Id { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; }

        [JsonPropertyName("icon")]
        public string Icon { get; init; }

        [JsonPropertyName("owner_id")]
        public string OwnerId { get; init; }

        [JsonPropertyName("member_count")]
        public int MemberCount { get; init; }

        [JsonPropertyName("symbol")]
        public string Symbol { get; init; }

        [JsonPropertyName("roles")]
        public List<GuildRole> Roles { get; init; }
    }
}
