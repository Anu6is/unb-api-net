using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnbelievaBoat.Net.Models
{
    public record GuildLeaderboard
    {
        [JsonPropertyName("guild_id")]
        public string GuildId { get; init; }

        [JsonPropertyName("users")]
        public List<UserBalance> Users { get; init; }
    }
}
