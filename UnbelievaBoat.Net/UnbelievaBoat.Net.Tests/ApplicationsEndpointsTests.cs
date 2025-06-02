using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnbelievaBoat.Net.Models;
using Xunit;
using FluentAssertions;
using System.Text.Json;
using UnbelievaBoat.Net;
using System.Collections.Generic; // Required for List<string>

public class ApplicationsEndpointsTests : IDisposable
{
    private const string DummyToken = "test_token";
    private const string DefaultTestBaseUrl = "http://localhost";
    private const string ApiPathPrefix = "/api/v1";
    private HttpClient _mockHttpClient;
    private MockHttpMessageHandler _mockHttpMessageHandler;

    private UnbelievaBoatClient CreateTestClient(MockHttpMessageHandler handler)
    {
        _mockHttpMessageHandler = handler;
        _mockHttpClient = new HttpClient(_mockHttpMessageHandler);
        return new UnbelievaBoatClient(DummyToken, $"{DefaultTestBaseUrl}{ApiPathPrefix}", httpClient: _mockHttpClient);
    }

    [Fact]
    public async Task GetApplicationPermissionsAsync_ValidRequest_ReturnsPermissions()
    {
        var expectedPermissions = new ApplicationPermissions { Permissions = new List<string> { "perm1", "perm2" } };
        var jsonResponse = JsonSerializer.Serialize(expectedPermissions, UnbelievaBoatClient.DefaultJsonSerializerOptions);

        var client = CreateTestClient(MockHttpMessageHandler.Create(HttpStatusCode.OK, jsonResponse, req =>
        {
            req.Method.Should().Be(HttpMethod.Get);
            req.RequestUri.AbsolutePath.Should().Be($"{ApiPathPrefix}/applications/@me/permissions");
        }));

        var result = await client.GetApplicationPermissionsAsync();

        result.Should().BeEquivalentTo(expectedPermissions);
    }

    public void Dispose()
    {
        _mockHttpClient?.Dispose();
    }
}
