Imports Believe.Net
Namespace Believe.Net
    Public NotInheritable Class UnbelievaClientConfig
        ''' <summary>
        '''     API Access Token
        ''' </summary>
        Public Property Token As String
        Public ReadOnly Property ApiBaseUrl As String
            Get
                Return $"https://unbelievable.pizza/api/{ApiVersion}"
            End Get
        End Property
        ''' <summary>
        '''     Version of the API being targeted
        ''' </summary>
        Public Property ApiVersion As String = "v1"
        Public Property LogLevel As LogLevel = LogLevel.Verbose
        ''' <summary>
        '''     Whether or not to resend the request when the ratelimit is reached
        ''' </summary>
        Public Property RetryOnRateLimit As Boolean = False
    End Class
End Namespace

