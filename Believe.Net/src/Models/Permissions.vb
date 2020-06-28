Imports Newtonsoft.Json

Namespace Believe.Net.Models
    Public NotInheritable Class Permissions
        <JsonProperty("permissions")>
        Public Property RawValue As ULong


        Public Function Has(permission As ApplicationPermission) As Boolean
            Return (permission And RawValue) = permission
        End Function

        Public Function ToList() As List(Of ApplicationPermission)
            Dim perms = New List(Of ApplicationPermission) From {ApplicationPermission.AccessEconomy}

            For i As Byte = 0 To 52
                Dim flag As ULong = 1UL << i
                If (RawValue And flag) <> 0UL Then
                    perms.Add(CType(flag, ApplicationPermission))
                End If
            Next
            Return perms
        End Function

        Public Overrides Function ToString() As String
            Return RawValue.ToString
        End Function
    End Class
End Namespace