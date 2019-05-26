Imports System.IO
Imports System.Text
Imports Newtonsoft.Json

Namespace Believe.Net.Parameters
    Friend Class CashBalanceParameters
        Inherits RequestParametersBase

        <JsonProperty("cash")>
        Public Property Cash As Double
        <JsonProperty("reason")>
        Public Property Reason As String

        Friend Overrides Function Serialize(serializer As JsonSerializer) As String
            Dim sb As New StringBuilder
            Using text As TextWriter = New StringWriter(sb)
                Using writer As JsonWriter = New JsonTextWriter(text)
                    serializer.Serialize(writer, Me)
                End Using
            End Using
            Return sb.ToString
        End Function
    End Class
End Namespace

