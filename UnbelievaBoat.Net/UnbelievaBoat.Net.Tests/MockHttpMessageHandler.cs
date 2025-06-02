using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handlerFunc(request, cancellationToken);
    }

    public static MockHttpMessageHandler Create(HttpStatusCode statusCode, HttpContent content = null, Action<HttpRequestMessage> requestValidator = null)
    {
        return new MockHttpMessageHandler((req, token) =>
        {
            requestValidator?.Invoke(req);
            return Task.FromResult(new HttpResponseMessage(statusCode) { Content = content ?? new StringContent("") });
        });
    }

    public static MockHttpMessageHandler Create(HttpStatusCode statusCode, string jsonContent, Action<HttpRequestMessage> requestValidator = null)
    {
        var content = new StringContent(jsonContent ?? string.Empty, System.Text.Encoding.UTF8, "application/json");
        return Create(statusCode, content, requestValidator);
    }

    public static MockHttpMessageHandler CreateSequence(params Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>[] responseFactories)
    {
        var responseEnumerator = responseFactories.GetEnumerator();
        return new MockHttpMessageHandler((req, token) =>
        {
            if (responseEnumerator.MoveNext())
            {
                var currentFactory = (Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>)responseEnumerator.Current;
                return currentFactory(req, token);
            }
            throw new InvalidOperationException("No more responses configured in sequence for MockHttpMessageHandler.");
        });
    }
}
