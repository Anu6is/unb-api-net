Namespace Believe.Net.Models

    Public NotInheritable Class Leaderboard
        Implements IDataModel : Implements ILeaderboard : Implements ICollection(Of User)

        ''' <summary>
        '''     Collection of leaderboard users.  
        ''' </summary>
        Public ReadOnly Property Users As ICollection(Of User) Implements ILeaderboard.Users
        Public Property IsRateLimited As Boolean Implements IDataModel.IsRateLimited
        Public Property RetryAfter As TimeSpan Implements IDataModel.RetryAfter

        Public Sub New()
            Users = New List(Of User)
        End Sub

        Public Sub New(collection As ICollection(Of User))
            Users = collection
        End Sub

        Public ReadOnly Property Count As Integer Implements ICollection(Of User).Count
            Get
                Return Users.Count
            End Get
        End Property

        Public ReadOnly Property IsReadOnly As Boolean Implements ICollection(Of User).IsReadOnly
            Get
                Return False
            End Get
        End Property

        Public Sub Add(item As User) Implements ICollection(Of User).Add
            Users.Add(item)
        End Sub

        Public Sub Clear() Implements ICollection(Of User).Clear
            Users.Clear()
        End Sub

        Public Sub CopyTo(array() As User, arrayIndex As Integer) Implements ICollection(Of User).CopyTo
            Users.CopyTo(array, arrayIndex)
        End Sub

        Public Function Contains(item As User) As Boolean Implements ICollection(Of User).Contains
            Return Users.Contains(item)
        End Function

        Public Function Remove(item As User) As Boolean Implements ICollection(Of User).Remove
            Return Users.Remove(item)
        End Function

        Public Function GetEnumerator() As IEnumerator(Of User) Implements IEnumerable(Of User).GetEnumerator
            Return Users.GetEnumerator()
        End Function

        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Return Users.GetEnumerator()
        End Function
    End Class
End Namespace
