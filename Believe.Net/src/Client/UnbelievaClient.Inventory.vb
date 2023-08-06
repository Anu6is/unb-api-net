Imports Believe.Net.Models
Imports System.Net.Http

Namespace Believe.Net
    Partial Public Class UnbelievaClient
        Public Async Function GetInventoryItems(guildId As ULong, userId As ULong,
                                                Optional itemName As String = "",
                                                Optional limit As Short = 100,
                                                Optional page As Integer = 1,
                                                Optional sort As IEnumerable(Of InventorySortOrder) = Nothing) As Task(Of User)
            Return Await _requestClient.SendAsync(Of User)(HttpMethod.Get, $"guilds/{guildId}/users/{userId}/inventory").ConfigureAwait(False)
        End Function
    End Class
End Namespace