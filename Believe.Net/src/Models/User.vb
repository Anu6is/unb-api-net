Imports Newtonsoft.Json
Namespace Believe.Net.Models
    Public NotInheritable Class User
        'Leaderboard rank of the user. 
        Public Rank As String

        'User ID of the discord user
        <JsonProperty("user_id")>
        Public UserId As String

        'User's cash balance
        Public Cash As Double

        'User's bank balance
        Public Bank As Double

        'User's total balance
        Public Total As Double
    End Class
End Namespace

