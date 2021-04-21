Imports System.Net.Http
Imports Believe.Net.Models
Imports Believe.Net.Parameters

Namespace Believe.Net
    Public NotInheritable Class UnbelievaClient
        Private ReadOnly RequestClient As RequestClient
        Private ReadOnly _config As UnbelievaClientConfig

        Public Event RequestLog(message As LogMessage)

        Public Sub New(token As String)
            MyClass.New(New UnbelievaClientConfig() With {.Token = token})
        End Sub

        Public Sub New(config As UnbelievaClientConfig)
            If String.IsNullOrWhiteSpace(config.Token) Then Throw New ArgumentNullException("Token cannot be null or empty")

            _config = config
            RequestClient = New RequestClient(Me, _config)
        End Sub

        ''' <summary>
        ''' Retrieve basic guild information for the specified guild id
        ''' </summary>
        ''' <remarks>
        '''     NOTE: This uses an undocumented endpoint!!
        '''     Complex objects from the returned json are not mapped (Bot Member, User, Roles, Channels). 
        ''' </remarks>
        ''' <param name="guildId"></param>
        ''' <returns><see cref="GuildInfo"/></returns>
        Public Async Function GetGuildInfoAsync(guildId As ULong) As Task(Of GuildInfo)
            Return Await RequestClient.SendAsync(Of GuildInfo)(HttpMethod.Get, $"guilds/{guildId}").ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Retrieve the current API <see cref="Permissions"/> for the application
        ''' </summary>
        ''' <param name="guildId">The id of the guild from which to get the <see cref="Permissions"/></param>
        ''' <returns><see cref="Permissions"/></returns>
        Public Async Function GetPermissionsAsync(guildId As ULong) As Task(Of Permissions)
            Return Await RequestClient.SendAsync(Of Permissions)(HttpMethod.Get, $"applications/@me/guilds/{guildId}").ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Determine if the application has a specific API permission
        ''' </summary>
        ''' <param name="guildId">The id of the guild to perform the permission check</param>
        ''' <param name="permission">The <see cref="ApplicationPermission"/> to check for</param>
        ''' <returns>True/False for if the permission is granted</returns>
        Public Async Function HasPermissionAsync(guildId As ULong, permission As ApplicationPermission) As Task(Of Boolean)
            Dim permissions = Await GetPermissionsAsync(guildId)
            Return permissions.Has(permission)
        End Function

        ''' <summary>
        ''' Retrieve the balance for a specified user
        ''' </summary>
        ''' <param name="guildId">The id of the guild the user belong to</param>
        ''' <param name="userId">The id of the user to be retrieved</param>
        ''' <returns><see cref="User"/></returns>
        Public Async Function GetUserBalanceAsync(guildId As ULong, userId As ULong) As Task(Of User)
            Return Await RequestClient.SendAsync(Of User)(HttpMethod.Get, $"guilds/{guildId}/users/{userId}").ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Set a user's cash balance to the amount specified
        ''' </summary>
        ''' <param name="guildId">The id of the guild the user belong to</param>
        ''' <param name="userId">The id of the user to be overridden</param>
        ''' <param name="cash">The value to set the cash balance to</param>
        ''' <param name="reason">Reason for the audit log</param>
        ''' <returns><see cref="User"/></returns>
        Public Async Function SetUserCashAsync(guildId As ULong, userId As ULong,
                                               cash As Decimal, Optional reason As String = Nothing) As Task(Of User)
            Dim params = New CashBalanceParameters() With {.Cash = cash, .Reason = reason}
            Return Await RequestClient.SendAsync(Of User)(HttpMethod.Put, $"guilds/{guildId}/users/{userId}", params).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Set a user's bank balance to the amount specified
        ''' </summary>
        ''' <param name="guildId">The id of the guild the user belong to</param>
        ''' <param name="userId">The id of the user to be overridden</param>
        ''' <param name="bank">The value to set the bank balance to</param>
        ''' <param name="reason">Reason for the audit log</param>
        ''' <returns><see cref="User"/></returns>
        Public Async Function SetUserBankAsync(guildId As ULong, userId As ULong,
                                               bank As Decimal, Optional reason As String = Nothing) As Task(Of User)
            Dim params = New BankBalanceParameters() With {.Bank = bank, .Reason = reason}
            Return Await RequestClient.SendAsync(Of User)(HttpMethod.Put, $"guilds/{guildId}/users/{userId}", params).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Set a user's balances to the amounts specified
        ''' </summary>
        ''' <param name="guildId">The id of the guild the user belong to</param>
        ''' <param name="userId">The id of the user to be overridden</param>
        ''' <param name="cash">The value to set the cash balance to</param>
        ''' <param name="bank">The value to set the bank balance to</param>
        ''' <param name="reason">Reason for the audit log</param>
        ''' <returns><see cref="User"/></returns>
        Public Async Function SetUserBalanceAsync(guildId As ULong, userId As ULong,
                                                  cash As Decimal, bank As Decimal, Optional reason As String = Nothing) As Task(Of User)
            Dim params = New UserBalanceParameters() With {.Cash = cash, .Bank = bank, .Reason = reason}
            Return Await RequestClient.SendAsync(Of User)(HttpMethod.Put, $"guilds/{guildId}/users/{userId}", params).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Increase a user's cash balance by the amount specified
        ''' </summary>
        ''' <param name="guildId">The id of the guild the user belong to</param>
        ''' <param name="userId">The id of the user to be overridden</param>
        ''' <param name="cash">The amount by which the cash balance should be increased</param>
        ''' <param name="reason">Reason for the audit log</param>
        ''' <returns><see cref="User"/></returns>
        Public Async Function IncreaseUserCashAsync(guildId As ULong, userId As ULong,
                                                    cash As Decimal, Optional reason As String = Nothing) As Task(Of User)
            Dim params = New CashBalanceParameters() With {.Cash = cash, .Reason = reason}
            Return Await RequestClient.SendAsync(Of User)(New HttpMethod("PATCH"), $"guilds/{guildId}/users/{userId}", params).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Increase a user's bank balance by the amount specified
        ''' </summary>
        ''' <param name="guildId">The id of the guild the user belong to</param>
        ''' <param name="userId">The id of the user to be overridden</param>
        ''' <param name="bank">The amount by which the bank balance should be increased</param>
        ''' <param name="reason">Reason for the audit log</param>
        ''' <returns><see cref="User"/></returns>
        Public Async Function IncreaseUserBankAsync(guildId As ULong, userId As ULong,
                                                    bank As Decimal, Optional reason As String = Nothing) As Task(Of User)
            Dim params = New BankBalanceParameters() With {.Bank = bank, .Reason = reason}
            Return Await RequestClient.SendAsync(Of User)(New HttpMethod("PATCH"), $"guilds/{guildId}/users/{userId}", params).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Increase a user's balances by the amounts specified
        ''' </summary>
        ''' <param name="guildId">The id of the guild the user belong to</param>
        ''' <param name="userId">The id of the user to be overridden</param>
        ''' <param name="cash">The amount by which the cash balance should be increased</param>
        ''' <param name="bank">The amount by which the bank balance should be increased</param>
        ''' <param name="reason">Reason for the audit log</param>
        ''' <returns><see cref="User"/></returns>
        Public Async Function IncreaseUserBalanceAsync(guildId As ULong, userId As ULong,
                                                       cash As Decimal, bank As Decimal, Optional reason As String = Nothing) As Task(Of User)
            Dim params = New UserBalanceParameters() With {.Cash = cash, .Bank = bank, .Reason = reason}
            Return Await RequestClient.SendAsync(Of User)(New HttpMethod("PATCH"), $"guilds/{guildId}/users/{userId}", params).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Decrease a user's cash balance by the amount specified
        ''' </summary>
        ''' <param name="guildId">The id of the guild the user belong to</param>
        ''' <param name="userId">The id of the user to be overridden</param>
        ''' <param name="cash">The amount by which the cash balance should be decreased</param>
        ''' <param name="reason">Reason for the audit log</param>
        ''' <returns><see cref="User"/></returns>
        Public Async Function DecreaseUserCashAsync(guildId As ULong, userId As ULong,
                                                    cash As Decimal, Optional reason As String = Nothing) As Task(Of User)
            Dim params = New CashBalanceParameters() With {.Cash = Math.Abs(cash) * -1, .Reason = reason}
            Return Await RequestClient.SendAsync(Of User)(New HttpMethod("PATCH"), $"guilds/{guildId}/users/{userId}", params).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Decrease a user's bank balance by the amount specified
        ''' </summary>
        ''' <param name="guildId">The id of the guild the user belong to</param>
        ''' <param name="userId">The id of the user to be overridden</param>
        ''' <param name="bank">The amount by which the bank balance should be decreased</param>
        ''' <param name="reason">Reason for the audit log</param>
        ''' <returns><see cref="User"/></returns>
        Public Async Function DecreaseUserBankAsync(guildId As ULong, userId As ULong,
                                                    bank As Decimal, Optional reason As String = Nothing) As Task(Of User)
            Dim params = New BankBalanceParameters() With {.Bank = Math.Abs(bank) * -1, .Reason = reason}
            Return Await RequestClient.SendAsync(Of User)(New HttpMethod("PATCH"), $"guilds/{guildId}/users/{userId}", params).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Decrease a user's balances by the amounts specified
        ''' </summary>
        ''' <param name="guildId">The id of the guild the user belong to</param>
        ''' <param name="userId">The id of the user to be overridden</param>
        ''' <param name="cash">The amount by which the cash balance should be decreased</param>
        ''' <param name="bank">The amount by which the bank balance should be decreased</param>
        ''' <param name="reason">Reason for the audit log</param>
        ''' <returns><see cref="User"/></returns>
        Public Async Function DecreaseUserBalanceAsync(guildId As ULong, userId As ULong,
                                                       cash As Decimal, bank As Decimal, Optional reason As String = Nothing) As Task(Of User)
            Dim params = New UserBalanceParameters() With {.Cash = Math.Abs(cash) * -1, .Bank = Math.Abs(bank) * -1, .Reason = reason}
            Return Await RequestClient.SendAsync(Of User)(New HttpMethod("PATCH"), $"guilds/{guildId}/users/{userId}", params).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Retrieve the leaderboard for the specified guild
        ''' </summary>
        ''' <param name="guildId">The id of the guild to retrieve</param>
        ''' <param name="sort">The value to sort the leaderboard by. <see cref="SortOrder"/> (cash, bank or total).</param>
        ''' <param name="limit">The limit of items to return.</param>
        ''' <param name="offset">The index at which to start retrieving items from the leaderboard.</param>
        ''' <param name="page">The page to get items from.</param>
        ''' <returns>List of <see cref="User"/></returns>
        Public Async Function GetGuildLeaderboardAsync(guildId As ULong,
                                                       Optional sort As SortOrder = SortOrder.total,
                                                       Optional limit As Short? = Nothing,
                                                       Optional offset As Integer = 1,
                                                       Optional page As Integer? = Nothing) As Task(Of ILeaderboard)
            If page IsNot Nothing AndAlso limit Is Nothing Then limit = 1000
            Dim query = New LeaderboardParameters() With {.Sort = [Enum].GetName(GetType(SortOrder), sort),
                                                           .Limit = limit, .Offset = offset, .Page = page}

            If page Is Nothing Then
                Return Await RequestClient.SendAsync(Of Leaderboard)(HttpMethod.Get, $"guilds/{guildId}/users", queryString:=query.ToString).ConfigureAwait(False)
            Else
                Return Await RequestClient.SendAsync(Of LeaderboardPage)(HttpMethod.Get, $"guilds/{guildId}/users", queryString:=query.ToString).ConfigureAwait(False)
            End If
        End Function

        Friend Function LogAsync(message As LogMessage) As Task
            If message.Level >= _config.LogLevel Then RaiseEvent RequestLog(message)
            Return Task.CompletedTask
        End Function
    End Class
End Namespace
