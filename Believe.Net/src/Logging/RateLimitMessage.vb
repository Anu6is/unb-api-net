Imports Believe.Net.Models

Namespace Believe.Net
    Friend NotInheritable Class RatelimitMessage
        Inherits LogMessage

        ''' <summary>
        '''     The total number of request that can be made
        ''' </summary>
        Public ReadOnly Property RateLimit As Integer
        ''' <summary>
        '''     The available number of requests before the limit is 
        ''' </summary>
        Public ReadOnly Property Remaining As Integer
        ''' <summary>
        '''     Time at which the limit will be reset
        ''' </summary>
        Public ReadOnly Property Reset As DateTimeOffset

        Public Sub New(level As LogLevel, message As String, info As RateLimitInfo)
            MyBase.New(level, message)
            RateLimit = info.Limit.GetValueOrDefault
            Remaining = info.Remaining.GetValueOrDefault
            Reset = DateTimeOffset.FromUnixTimeMilliseconds(info.Reset.GetValueOrDefault)
        End Sub
    End Class
End Namespace

