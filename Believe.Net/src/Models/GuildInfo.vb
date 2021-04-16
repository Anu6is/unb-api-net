Imports Newtonsoft.Json

Namespace Believe.Net.Models
    Public Class GuildInfo : Implements IDataModel
        <JsonProperty("id")>
        Public Property Id As ULong
        <JsonProperty("name")>
        Public Property Name As String
        <JsonProperty("icon")>
        Public Property Icon As String
        <JsonProperty("owner_id")>
        Public Property OwnerId As ULong
        <JsonProperty("member_count")>
        Public Property MemberCount As Integer
        <JsonProperty("symbol")>
        Public Property Symbol As String
        <JsonProperty("premium")>
        Public Property Premium As Boolean
        <JsonProperty("max_role_income")>
        Public Property MaxRoleIncome As ULong

        <JsonIgnore>
        Public Property IsRateLimited As Boolean Implements IDataModel.IsRateLimited
        <JsonIgnore>
        Public Property RetryAfter As TimeSpan Implements IDataModel.RetryAfter
    End Class
End Namespace
