Imports System.Net.Http
Imports Believe.Net.Models
Imports Believe.Net.Parameters
Imports Microsoft.Extensions.Logging

Namespace Believe.Net
    Partial Public NotInheritable Class UnbelievaClient
        Private ReadOnly _logger As ILogger
        Private ReadOnly _requestClient As RequestClient

        Public Sub New(config As UnbelievaClientConfig, logger As ILogger(Of UnbelievaClient))
            If String.IsNullOrWhiteSpace(config.Token) Then Throw New ArgumentNullException("Token cannot be null or empty")

            _logger = logger
            _requestClient = New RequestClient(Me, config)
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
            Return Await _requestClient.SendAsync(Of GuildInfo)(HttpMethod.Get, $"guilds/{guildId}").ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Retrieve the current API <see cref="Permissions"/> for the application
        ''' </summary>
        ''' <param name="guildId">The id of the guild from which to get the <see cref="Permissions"/></param>
        ''' <returns><see cref="Permissions"/></returns>
        Public Async Function GetPermissionsAsync(guildId As ULong) As Task(Of Permissions)
            Return Await _requestClient.SendAsync(Of Permissions)(HttpMethod.Get, $"applications/@me/guilds/{guildId}").ConfigureAwait(False)
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


        Friend Function LogAsync(log As LogMessage) As Task
            If _logger Is Nothing Then Return Task.CompletedTask

            Select Case log.Level
                Case LogLevel.Critical : _logger.LogCritical(log.Message)
                Case LogLevel.Error : _logger.LogError(log.Message)
                Case LogLevel.Warning : _logger.LogWarning(log.Message)
                Case LogLevel.Info : _logger.LogInformation(log.Message)
                Case LogLevel.Debug : _logger.LogDebug(log.Message)
            End Select

            Return Task.CompletedTask
        End Function
    End Class
End Namespace
