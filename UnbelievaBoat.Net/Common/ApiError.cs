using System.Net;
using System.Net.Http.Headers; // For HttpResponseHeaders
using System.Collections.Generic; // For IReadOnlyDictionary

namespace UnbelievaBoat.Net.Common
{
    public record ApiError
    {
        /// <summary>
        /// The HTTP status code returned by the API.
        /// </summary>
        public HttpStatusCode StatusCode { get; init; }

        /// <summary>
        /// The error message, potentially parsed from the API response body.
        /// </summary>
        public string Message { get; init; }

        /// <summary>
        /// The raw content of the error response, if available.
        /// </summary>
        public string RawContent { get; init; }

        /// <summary>
        /// Response headers associated with the error response.
        /// Can be null if headers were not captured or relevant.
        /// </summary>
        public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; init; }


        public ApiError(HttpStatusCode statusCode, string message, string rawContent = null, IReadOnlyDictionary<string, IEnumerable<string>> headers = null)
        {
            StatusCode = statusCode;
            Message = message ?? "An unknown API error occurred."; // Ensure message is not null
            RawContent = rawContent;
            Headers = headers;
        }
    }

    // Example of a more specific error, could be added later if needed:
    // public record RateLimitApiError : ApiError
    // {
    //     public TimeSpan? RetryAfter { get; init; }
    //     public RateLimitApiError(HttpStatusCode statusCode, string message, string rawContent, IReadOnlyDictionary<string, IEnumerable<string>> headers, TimeSpan? retryAfter)
    //         : base(statusCode, message, rawContent, headers)
    //     {
    //         RetryAfter = retryAfter;
    //     }
    // }
}
