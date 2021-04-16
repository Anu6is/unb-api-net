Namespace Believe.Net.Models
    Public Interface IDataModel
        Property IsRateLimited As Boolean
        Property RetryAfter As TimeSpan
    End Interface
End Namespace