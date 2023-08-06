Imports Newtonsoft.Json

Namespace Believe.Net.Models
    Public NotInheritable Class LeaderboardPage : Implements IDataModel : Implements ILeaderboard
        ''' <summary>
        '''     Collection of leaderboard users.  
        ''' </summary>
        <JsonProperty("users")>
        Public ReadOnly Property Users As ICollection(Of User) Implements ILeaderboard.Users
        ''' <summary>
        '''     Current leaderboad page.
        ''' </summary>
        <JsonProperty("page")>
        Public Property Page As Integer
        ''' <summary>
        '''     Total leaderboard pages available
        ''' </summary>
        <JsonProperty("total_pages")>
        Public Property TotalPages As Integer
        Public Property IsRateLimited As Boolean Implements IDataModel.IsRateLimited
        Public Property RetryAfter As TimeSpan Implements IDataModel.RetryAfter
    End Class
End Namespace