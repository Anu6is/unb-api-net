
Imports Newtonsoft.Json

Namespace Believe.Net.Models
    Friend NotInheritable Class RateLimit
        ''' <summary>
        '''     Rate limit message
        ''' </summary>
        <JsonProperty("message")>
        Public Message As String

        ''' <summary>
        '''     The number of milliseconds to wait before submitting another request.
        ''' </summary>
        <JsonProperty("retry_after")>
        Public RetryAfter As Long
    End Class
End Namespace

