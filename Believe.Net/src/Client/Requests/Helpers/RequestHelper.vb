Imports System.IO
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Believe.Net.Parameters
Imports Newtonsoft.Json

Namespace Believe.Net
    Friend Module RequestHelper
        <Extension>
        Friend Function Deserialize(Of T)(stream As Stream, serializer As JsonSerializer) As T
            If stream Is Nothing OrElse stream.CanRead = False Then Return Nothing

            Using reader = New StreamReader(stream)
                Using textReader = New JsonTextReader(reader)
                    Return serializer.Deserialize(Of T)(textReader)
                End Using
            End Using
        End Function

        <Extension>
        Friend Async Function ToStringAsync(stream As Stream) As Task(Of String)
            Dim content As String = Nothing

            If stream IsNot Nothing Then
                Using reader = New StreamReader(stream)
                    content = Await reader.ReadToEndAsync()
                End Using
            End If

            Return content
        End Function

    End Module
End Namespace

