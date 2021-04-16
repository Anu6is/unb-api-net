Imports Newtonsoft.Json
Namespace Believe.Net.Models
    Public NotInheritable Class User : Implements IDataModel

        ''' <summary>
        '''     Leaderboard rank of the user.  
        ''' </summary>
        <JsonProperty("rank")>
        Public Rank As String

        ''' <summary>
        '''     User ID of the discord user 
        ''' </summary>
        <JsonProperty("user_id")>
        Public UserId As String

        ''' <summary>
        '''     User's cash balance
        ''' </summary>
        <JsonProperty("cash")>
        Public Cash As Double

        ''' <summary>
        '''     User's bank balance 
        ''' </summary>
        <JsonProperty("bank")>
        Public Bank As Double

        ''' <summary>
        '''     User's total balance
        ''' </summary>
        <JsonProperty("total")>
        Public Total As Double

        <JsonIgnore>
        Public Property IsRateLimited As Boolean Implements IDataModel.IsRateLimited
        <JsonIgnore>
        Public Property RetryAfter As TimeSpan Implements IDataModel.RetryAfter
    End Class
End Namespace

