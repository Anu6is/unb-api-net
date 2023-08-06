Imports Newtonsoft.Json

Namespace Believe.Net.Models
    Public NotInheritable Class InventoryItem : Inherits ItemBase
        <JsonProperty("item_id")>
        Public Property ItemId As ULong
        <JsonProperty("quantity")>
        Public Property Quantity As String
    End Class
End Namespace