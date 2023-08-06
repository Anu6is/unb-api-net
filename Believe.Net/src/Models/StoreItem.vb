Imports Newtonsoft.Json

Namespace Believe.Net.Models
    Public NotInheritable Class StoreItem : Inherits ItemBase
        <JsonProperty("id")>
        Public Property Id As ULong
        <JsonProperty("price")>
        Public Property Price As Double
        <JsonProperty("is_inventory")>
        Public Property IsInventory As Boolean
        <JsonProperty("stock_remaining")>
        Public Property StockRemaining As Integer?
        <JsonProperty("unlimited_stock")>
        Public Property UnlimitedStock As Boolean
        <JsonProperty("expires_at")>
        Public Property ExpiresAt As DateTimeOffset?
    End Class
End Namespace