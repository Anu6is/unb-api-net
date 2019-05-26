Imports System.IO
Imports Newtonsoft.Json

Namespace Believe.Net.Parameters
    Public MustInherit Class RequestParametersBase
        Friend Overridable Property Encoding As String = "application/json"
        Friend MustOverride Function Serialize(serializer As JsonSerializer) As String
    End Class
End Namespace

