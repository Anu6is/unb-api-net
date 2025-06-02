using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnbelievaBoat.Net.Models;
using Xunit;
using FluentAssertions;
using System.Text.Json;
using UnbelievaBoat.Net;

public class UnbelievaBoatClientTests : IDisposable
{
    private const string DummyToken = "test_token";
    private const string DefaultTestBaseUrl = "http://localhost/api/v1"; // Test URL
    private HttpClient _mockHttpClient;
    private MockHttpMessageHandler _mockHttpMessageHandler;


    [Fact]
    public async Task SendAsync_ShouldIncludeAuthorizationHeader()
    {
        string capturedAuthHeader = null;
        _mockHttpMessageHandler = MockHttpMessageHandler.Create(HttpStatusCode.OK, "{}", req =>
        {
            capturedAuthHeader = req.Headers.Authorization?.ToString();
        });
        _mockHttpClient = new HttpClient(_mockHttpMessageHandler); // BaseAddress will be set by UnbelievaBoatClient constructor
        var client = new UnbelievaBoatClient(DummyToken, DefaultTestBaseUrl, httpClient: _mockHttpClient);

        await client.GetApplicationPermissionsAsync();

        capturedAuthHeader.Should().NotBeNull();
        capturedAuthHeader.Should().Be($"Bearer {DummyToken}");
    }

    [Fact]
    public async Task Request_WhenApiReturnsError_ShouldThrowHttpRequestExceptionWithContent()
    {
        var errorJson = "{\"error\":\"Test API Error\",\"status\":400}";
        _mockHttpMessageHandler = MockHttpMessageHandler.Create(HttpStatusCode.BadRequest, errorJson);
        _mockHttpClient = new HttpClient(_mockHttpMessageHandler);
        var client = new UnbelievaBoatClient(DummyToken, DefaultTestBaseUrl, httpClient: _mockHttpClient);

        Func<Task> act = async () => await client.GetApplicationPermissionsAsync();

        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.And.Message.Should().Contain("400 (Bad Request)"); // Default ReasonPhrase for 400
        exception.And.Message.Should().Contain(errorJson);
    }

    [Fact]
    public async Task Request_WhenServerReturnsTransientError_ShouldRetry()
    {
        // Arrange
        var attempts = 0;
        _mockHttpMessageHandler = MockHttpMessageHandler.CreateSequence(
            (req, token) => { attempts++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)); }, // First attempt: 503
            (req, token) => { attempts++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)); }, // Second attempt: 503
            (req, token) => { attempts++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") }); }  // Third attempt: 200 OK
        );
        _mockHttpClient = new HttpClient(_mockHttpMessageHandler); // BaseAddress will be set by UnbelievaBoatClient

        var retryConfig = new RetryPolicyConfig { MaxRetries = 2, InitialDelay = TimeSpan.FromMilliseconds(10) }; // Fast retries for test. MaxRetries = 2 means 1 initial + 2 retries = 3 attempts.
        var client = new UnbelievaBoatClient(DummyToken, DefaultTestBaseUrl, retryConfig, _mockHttpClient);

        // Act
        // Using GetApplicationPermissionsAsync as a sample endpoint call
        await client.GetApplicationPermissionsAsync();

        // Assert
        attempts.Should().Be(3); // Should have made 3 attempts
    }

    [Fact]
    public async Task Request_WhenRateLimited_ShouldDelayBasedOnResetAfterHeader()
    {
        var resetAfterSeconds = 1; // Test with a short delay
        var callCount = 0;
        var firstCallTime = DateTimeOffset.MinValue;
        var secondCallTime = DateTimeOffset.MinValue;

        _mockHttpMessageHandler = MockHttpMessageHandler.CreateSequence(
            (req, ct) => { // First call, returns rate limit headers indicating limit reached
                callCount++;
                firstCallTime = DateTimeOffset.UtcNow;
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") };
                response.Headers.Add("X-RateLimit-Limit", "100");
                response.Headers.Add("X-RateLimit-Remaining", "0"); // Limit reached
                response.Headers.Add("X-RateLimit-Reset-After", resetAfterSeconds.ToString());
                return Task.FromResult(response);
            },
            (req, ct) => { // Second call, should happen after delay
                callCount++;
                secondCallTime = DateTimeOffset.UtcNow;
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") };
                response.Headers.Add("X-RateLimit-Limit", "100");
                response.Headers.Add("X-RateLimit-Remaining", "99");
                response.Headers.Add("X-RateLimit-Reset-After", "60");
                return Task.FromResult(response);
            }
        );
        _mockHttpClient = new HttpClient(_mockHttpMessageHandler);
        // No retry for this specific test, focus on rate limiter's delay
        var client = new UnbelievaBoatClient(DummyToken, DefaultTestBaseUrl, new RetryPolicyConfig { MaxRetries = 0 }, _mockHttpClient);

        // Act
        await client.GetApplicationPermissionsAsync(); // First call
        await client.GetApplicationPermissionsAsync(); // Second call, should be delayed by rate limiter

        // Assert
        callCount.Should().Be(2);
        secondCallTime.Should().BeCloseTo(firstCallTime.AddSeconds(resetAfterSeconds), TimeSpan.FromMilliseconds(500)); // Allow some margin for execution
    }

    public void Dispose()
    {
        _mockHttpClient?.Dispose();
        // _mockHttpMessageHandler does not need explicit dispose unless it holds disposable resources
    }
}
