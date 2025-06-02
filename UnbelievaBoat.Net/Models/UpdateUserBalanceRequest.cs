using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace UnbelievaBoat.Net.Models
{
    public record UpdateUserBalanceRequest
    {
        [JsonPropertyName("cash")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? Cash { get; set; }

        [JsonPropertyName("bank")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? Bank { get; set; }

        [JsonPropertyName("reason")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Reason { get; set; }
    }
}
