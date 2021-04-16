Imports Newtonsoft.Json
Namespace Believe.Net.Models
    Public NotInheritable Class Leaderboard : Implements IDataModel
        ''' <summary>
        '''     Sorted list of leaderboard users.  
        ''' </summary>
        <JsonProperty("users")>
        Public Property Users As User()

        ''' <summary>
        '''     The total number of pages.  
        ''' </summary>
        <JsonProperty("total_pages")>
        Public Property TotalPages As Integer

        <JsonIgnore>
        Public Property IsRateLimited As Boolean Implements IDataModel.IsRateLimited
        <JsonIgnore>
        Public Property RetryAfter As TimeSpan Implements IDataModel.RetryAfter
    End Class
End Namespace
