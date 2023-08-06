Imports Newtonsoft.Json

Namespace Believe.Net.Models
    Public NotInheritable Class ItemRequirement
        <JsonProperty("type")>
        Public Property Type As RequirementType
        <JsonProperty("match_type")>
        Public Property MatchType As MatchType
        <JsonProperty("ids")>
        Public Property Ids As List(Of ULong)
        <JsonProperty("balance")>
        Public Property Balance As Double?
    End Class
End Namespace