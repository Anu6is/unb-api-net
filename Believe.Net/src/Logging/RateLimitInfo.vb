Imports System.Text

Namespace Believe.Net.Models
    Friend Class RateLimitInfo

        ''' <summary>
        '''     The total number of requests that can be made
        ''' </summary>
        Public ReadOnly Limit As Integer?
        ''' <summary>
        '''     The number of remaining requests that can be made
        ''' </summary>
        Public ReadOnly Remaining As Integer?
        ''' <summary>
        '''     Epoch time at which the rate limit resets
        ''' </summary>
        Public ReadOnly Reset As Long?

        Friend Sub New(headers As Dictionary(Of String, String))
            Dim value As String = String.Empty

            Dim limit As Integer
            Me.Limit = If(headers.TryGetValue("X-RateLimit-Limit", value) AndAlso Integer.TryParse(value, limit), limit, CType(Nothing, Integer?))

            Dim remaining As Integer
            Me.Remaining = If(headers.TryGetValue("X-RateLimit-Remaining", value) AndAlso Integer.TryParse(value, remaining), remaining, CType(Nothing, Integer?))

            Dim reset As Long
            Me.Reset = If(headers.TryGetValue("X-RateLimit-Reset", value) AndAlso Long.TryParse(value, reset), reset, CType(Nothing, Long?))
        End Sub

        Public Overrides Function ToString() As String
            Return $"Requests: [{Remaining.GetValueOrDefault}/{Limit.GetValueOrDefault}] | Resets @ {DateTimeOffset.FromUnixTimeMilliseconds(Reset.GetValueOrDefault)}"
        End Function
    End Class
End Namespace
