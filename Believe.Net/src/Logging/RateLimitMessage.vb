Imports Believe.Net.Models

Namespace Believe.Net
    Friend NotInheritable Class RatelimitMessage
        Inherits LogMessage

        Public ReadOnly Info As RateLimitInfo

        Public Sub New(level As LogLevel, message As String, info As RateLimitInfo)
            MyBase.New(level, message)
            Me.Info = info
        End Sub

        Public Overrides Function ToString() As String
            Return $"{Message}{vbNewLine}{Info}"
        End Function
    End Class
End Namespace

