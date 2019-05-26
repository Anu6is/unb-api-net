Imports System.IO
Imports Newtonsoft.Json

Namespace Believe.Net.Parameters
    Friend Class DefaultParameters
        Inherits RequestParametersBase

        Friend Overrides Function Serialize(serializer As JsonSerializer) As String
            Return String.Empty
        End Function

        Public Shared Function Empty() As DefaultParameters
            Return New DefaultParameters
        End Function
    End Class
End Namespace

