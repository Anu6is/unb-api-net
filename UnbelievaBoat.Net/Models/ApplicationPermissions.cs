using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnbelievaBoat.Net.Models
{
    public record ApplicationPermissions
    {
        [JsonPropertyName("permissions")]
        public List<string> Permissions { get; init; }
    }
}
