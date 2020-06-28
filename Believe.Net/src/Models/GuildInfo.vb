Imports Newtonsoft.Json

Namespace Believe.Net.Models
    Public Class GuildInfo
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
    End Class
End Namespace
