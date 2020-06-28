Imports System.Net
Imports Believe.Net.Models

Namespace Believe.Net
    Friend NotInheritable Class RequestMessage
        Inherits LogMessage

        ''' <summary>
        '''     The request endpoint
        ''' </summary>
        Private ReadOnly Request As String
        ''' <summary>
        '''     The status code returned by the API
        ''' </summary>
        Private ReadOnly Status As HttpStatusCode
        ''' <summary>
        '''     The length of time (ms) the reuquest took
        ''' </summary>
        Private ReadOnly Duration As Long
        ''' <summary>
        '''     Rate limit details
        ''' </summary>
        Private ReadOnly RateLimitInfo As RateLimitInfo

        Public Sub New(level As LogLevel, message As String, request As RequestResponse)
            MyBase.New(level, message)

            Me.Request = request.Request
            Me.Status = request.StatusCode
            Me.Duration = request.Duration
            Me.RateLimitInfo = New RateLimitInfo(request.Headers)
        End Sub

        Public Overrides Function ToString() As String
            Return $"{Request} {Duration}ms | Status Code:{Status}{vbNewLine}{RateLimitInfo}"
        End Function
    End Class
End Namespace

