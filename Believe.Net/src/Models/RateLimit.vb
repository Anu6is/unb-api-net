
Imports Newtonsoft.Json

Namespace Believe.Net.Models
    Friend NotInheritable Class RateLimit
        'Rate limit message
        Public Message As String

        'The number of milliseconds to wait before submitting another request.
        <JsonProperty("retry_after")>
        Public RetryAfter As Long
    End Class
End Namespace

