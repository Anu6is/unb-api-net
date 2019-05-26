Imports Believe.Net.Models

Namespace Believe.Net
    Friend NotInheritable Class RatelimitMessage
        Inherits LogMessage
        Public ReadOnly Property RateLimit As Integer       'The total number of request that can be made
        Public ReadOnly Property Remaining As Integer       'The available number of requests before the limit is reached
        Public ReadOnly Property Reset As DateTimeOffset    'Time at which the limit will be reset

        Public Sub New(level As LogLevel, message As String, info As RateLimitInfo)
            MyBase.New(level, message)
            RateLimit = info.Limit.GetValueOrDefault
            Remaining = info.Remaining.GetValueOrDefault
            Reset = DateTimeOffset.FromUnixTimeMilliseconds(info.Reset.GetValueOrDefault)
        End Sub
    End Class
End Namespace

