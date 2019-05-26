Imports System.Net
Imports Believe.Net.Models

Namespace Believe.Net
    Friend NotInheritable Class RequestMessage
        Inherits LogMessage

        Private ReadOnly Request As String              'The request endpoint
        Private ReadOnly Status As HttpStatusCode       'The status code returned by the API
        Private ReadOnly Duration As Long               'The length of time (ms) the reuquest took
        Private ReadOnly RateLimitInfo As RateLimitInfo 'Rate limit details

        Public Sub New(level As LogLevel, message As String, request As RequestResponse)
            MyBase.New(level, message)

            Me.Request = request.Request
            Me.Status = request.StatusCode
            Me.Duration = request.Duration
            Me.RateLimitInfo = New RateLimitInfo(request.Headers)
        End Sub

        Public Function Resolve() As String
            Return $"{Request} {Duration}ms | Status Code:{Status}{vbNewLine}{RateLimitInfo.ToString}"
        End Function
    End Class
End Namespace

