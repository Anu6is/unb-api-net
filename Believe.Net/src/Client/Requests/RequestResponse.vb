Imports System.IO
Imports System.Net

Namespace Believe.Net
    Friend Structure RequestResponse
        Public ReadOnly Property Request As String
        Public ReadOnly Property StatusCode As HttpStatusCode
        Public ReadOnly Property Headers As Dictionary(Of String, String)
        Public ReadOnly Property Stream As Stream
        Public ReadOnly Property Duration As Long
        Public Property Message As String
        Public Property Retry As TimeSpan?
        Public ReadOnly Property IsSuccess As Boolean
            Get
                Return StatusCode = HttpStatusCode.OK
            End Get
        End Property

        Public Sub New(request As String, statusCode As HttpStatusCode, headers As Dictionary(Of String, String), stream As Stream, duration As Long)
            Me.Request = request
            Me.StatusCode = statusCode
            Me.Headers = headers
            Me.Stream = stream
            Me.Duration = duration
        End Sub
    End Structure
End Namespace
