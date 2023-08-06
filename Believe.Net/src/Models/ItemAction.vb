Imports Newtonsoft.Json

Namespace Believe.Net.Models
    Public NotInheritable Class ItemAction
        <JsonProperty("type")>
        Public Property Type As ActionType
        <JsonProperty("message")>
        Public Property Message As Object
        <JsonProperty("ids")>
        Public Property Ids As List(Of ULong)
        <JsonProperty("balance")>
        Public Property Balance As Double?
    End Class
End Namespace