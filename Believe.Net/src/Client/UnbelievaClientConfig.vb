Imports Believe.Net
Namespace Believe.Net
    Public NotInheritable Class UnbelievaClientConfig
        Public Property Token As String
        Public ReadOnly Property ApiBaseUrl As String
            Get
                Return $"https://unbelievable.pizza/api/{ApiVersion}"
            End Get
        End Property
        Public Property ApiVersion As String = "v1"
        Public Property LogLevel As LogLevel = LogLevel.Verbose
        Public Property RetryOnRateLimit As Boolean = False
    End Class
End Namespace

