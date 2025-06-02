using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnbelievaBoat.Net.Models;
using Xunit;
using FluentAssertions;
using System.Text.Json;
using UnbelievaBoat.Net;
using System.Collections.Generic; // Required for List<string> in ApplicationPermissions

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
        // For GetApplicationPermissionsAsync to return a Success, it needs a valid JSON for ApplicationPermissions or an empty one if it's OkStatus
        var successPayload = new ApplicationPermissions { Permissions = new List<string>() };
        var successJson = JsonSerializer.Serialize(successPayload, UnbelievaBoatClient.DefaultJsonSerializerOptions);

        _mockHttpMessageHandler = MockHttpMessageHandler.Create(HttpStatusCode.OK, successJson, req =>
        {
            capturedAuthHeader = req.Headers.Authorization?.ToString();
        });
        _mockHttpClient = new HttpClient(_mockHttpMessageHandler);
        var client = new UnbelievaBoatClient(DummyToken, DefaultTestBaseUrl, httpClient: _mockHttpClient);

        var result = await client.GetApplicationPermissionsAsync();

        capturedAuthHeader.Should().NotBeNull();
        capturedAuthHeader.Should().Be($"Bearer {DummyToken}");
        result.IsSuccess.Should().BeTrue(); // Also check if the call itself was "successful" in terms of Result
    }

    [Fact]
    public async Task Request_WhenApiReturnsError_ShouldReturnFailureResultWithApiError()
    {
        var errorJson = "{\"error\":\"Test API Error Detail\",\"status\":400}"; // More specific content
        var expectedStatusCode = HttpStatusCode.BadRequest;
        _mockHttpMessageHandler = MockHttpMessageHandler.Create(expectedStatusCode, errorJson);
        _mockHttpClient = new HttpClient(_mockHttpMessageHandler);
        var client = new UnbelievaBoatClient(DummyToken, DefaultTestBaseUrl, httpClient: _mockHttpClient);

        // Act
        var result = await client.GetApplicationPermissionsAsync(); // Any method that returns Result<T, ApiError>

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.StatusCode.Should().Be(expectedStatusCode);
        result.Error.RawContent.Should().Be(errorJson);
        // Check against the specific message format from TryParseApiErrorAsync
        result.Error.Message.Should().Contain($"API request failed: BadRequest (Status: {expectedStatusCode}). Raw content: {errorJson}");
    }

    [Fact]
    public async Task Request_WhenServerReturnsTransientError_ShouldRetryAndSucceed() // Renamed for clarity
    {
        // Arrange
        var attempts = 0;
        var successPayload = new ApplicationPermissions { Permissions = new List<string> { "ok" } };
        var successJson = JsonSerializer.Serialize(successPayload, UnbelievaBoatClient.DefaultJsonSerializerOptions);

        _mockHttpMessageHandler = MockHttpMessageHandler.CreateSequence(
            (req, ct) => { attempts++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)); },
            (req, ct) => { attempts++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)); },
            (req, ct) => { attempts++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(successJson, System.Text.Encoding.UTF8, "application/json") }); }
        );
        _mockHttpClient = new HttpClient(_mockHttpMessageHandler);

        var retryConfig = new RetryPolicyConfig { MaxRetries = 2, InitialDelay = TimeSpan.FromMilliseconds(10) };
        var client = new UnbelievaBoatClient(DummyToken, DefaultTestBaseUrl, retryConfig, _mockHttpClient);

        // Act
        var result = await client.GetApplicationPermissionsAsync();

        // Assert
        attempts.Should().Be(3);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(successPayload);
    }

    [Fact]
    public async Task Request_WhenRateLimited_ShouldDelayAndSucceed() // Renamed for clarity
    {
        var resetAfterSeconds = 1;
        var callCount = 0;
        var firstCallTime = DateTimeOffset.MinValue;
        var secondCallTime = DateTimeOffset.MinValue;
        var successPayload = new ApplicationPermissions { Permissions = new List<string> { "ok" } };
        var successJson = JsonSerializer.Serialize(successPayload, UnbelievaBoatClient.DefaultJsonSerializerOptions);

        _mockHttpMessageHandler = MockHttpMessageHandler.CreateSequence(
            (req, ct) => {
                callCount++;
                firstCallTime = DateTimeOffset.UtcNow;
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(successJson, System.Text.Encoding.UTF8, "application/json") };
                response.Headers.Add("X-RateLimit-Limit", "100");
                response.Headers.Add("X-RateLimit-Remaining", "0");
                response.Headers.Add("X-RateLimit-Reset-After", resetAfterSeconds.ToString());
                return Task.FromResult(response);
            },
            (req, ct) => {
                callCount++;
                secondCallTime = DateTimeOffset.UtcNow;
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(successJson, System.Text.Encoding.UTF8, "application/json") };
                response.Headers.Add("X-RateLimit-Limit", "100");
                response.Headers.Add("X-RateLimit-Remaining", "99");
                response.Headers.Add("X-RateLimit-Reset-After", "60");
                return Task.FromResult(response);
            }
        );
        _mockHttpClient = new HttpClient(_mockHttpMessageHandler);
        var client = new UnbelievaBoatClient(DummyToken, DefaultTestBaseUrl, new RetryPolicyConfig { MaxRetries = 0 }, _mockHttpClient);

        // Act
        var result1 = await client.GetApplicationPermissionsAsync();
        var result2 = await client.GetApplicationPermissionsAsync();

        // Assert
        callCount.Should().Be(2);
        result1.IsSuccess.Should().BeTrue();
        result1.Value.Should().BeEquivalentTo(successPayload);
        result2.IsSuccess.Should().BeTrue();
        result2.Value.Should().BeEquivalentTo(successPayload);
        secondCallTime.Should().BeCloseTo(firstCallTime.AddSeconds(resetAfterSeconds), TimeSpan.FromMilliseconds(500));
    }

    public void Dispose()
    {
        _mockHttpClient?.Dispose();
    }
}
