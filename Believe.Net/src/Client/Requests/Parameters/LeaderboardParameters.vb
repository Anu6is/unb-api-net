Imports System.IO
Imports System.Text
Imports Newtonsoft.Json

Namespace Believe.Net.Parameters
    Friend Class LeaderboardParameters
        Inherits RequestParametersBase

        <JsonProperty("sort")>
        Public Property Sort As String
        <JsonProperty("limit")>
        Public Property Limit As Short?
        <JsonProperty("offset")>
        Public Property Offset As Integer
        <JsonProperty("page")>
        Public Property Page As Integer?

        Friend Overrides Function Serialize(serializer As JsonSerializer) As String
            Dim sb As New StringBuilder
            Using text As TextWriter = New StringWriter(sb)
                Using writer As JsonWriter = New JsonTextWriter(text)
                    serializer.Serialize(writer, Me)
                End Using
            End Using
            Return sb.ToString
        End Function

        Public Overrides Function ToString() As String
            Dim query As New StringBuilder("?")

            query.Append("sort=").Append(Sort)

            If Limit IsNot Nothing Then query.Append("&").Append("limit=").Append(Limit)
            If Page IsNot Nothing Then query.Append("&").Append("page=").Append(Page)

            query.Append("&").Append("offset=").Append(Offset)

            Return query.ToString()
        End Function
    End Class
End Namespace