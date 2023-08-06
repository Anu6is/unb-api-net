Imports Newtonsoft.Json

Namespace Believe.Net.Models
    Public Class ItemBase : Implements IDataModel
        <JsonProperty("name")>
        Public Property Name As String
        <JsonProperty("description")>
        Public Property Description As String
        <JsonProperty("is_usable")>
        Public Property IsUsable As Boolean
        <JsonProperty("is_sellable")>
        Public Property IsSellable As Boolean
        <JsonProperty("requirements")>
        Public Property Requirements As List(Of ItemRequirement)
        <JsonProperty("actions")>
        Public Property Actions As List(Of ItemAction)
        <JsonProperty("emoji_unicode")>
        Public Property EmojiUnicode As String
        <JsonProperty("emoji_id")>
        Public Property EmojiId As ULong?

        <JsonIgnore>
        Public Property IsRateLimited As Boolean Implements IDataModel.IsRateLimited
        <JsonIgnore>
        Public Property RetryAfter As TimeSpan Implements IDataModel.RetryAfter
    End Class
End Namespace