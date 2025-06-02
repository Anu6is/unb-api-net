using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnbelievaBoat.Net.Models;
using Xunit;
using FluentAssertions;
using System.Text.Json;
using UnbelievaBoat.Net;
using System.Linq;
using System.Collections.Generic;

public class EconomyEndpointsTests : IDisposable
{
    private const string DummyToken = "test_token";
    private const string DefaultTestBaseUrl = "http://localhost"; // Base part of URL
    private const string ApiPathPrefix = "/api/v1"; // API path prefix
    private HttpClient _mockHttpClient;
    private MockHttpMessageHandler _mockHttpMessageHandler;

    private UnbelievaBoatClient CreateTestClient(MockHttpMessageHandler handler)
    {
        _mockHttpMessageHandler = handler;
        _mockHttpClient = new HttpClient(_mockHttpMessageHandler);
        // Pass full base URL to client constructor
        return new UnbelievaBoatClient(DummyToken, $"{DefaultTestBaseUrl}{ApiPathPrefix}", httpClient: _mockHttpClient);
    }


    [Fact]
    public async Task GetUserBalanceAsync_ValidRequest_ReturnsUserBalance()
    {
        var guildId = "guild123";
        var userId = "user456";
        var expectedBalance = new UserBalance { UserId = userId, GuildId = guildId, Cash = 100, Bank = 500, Total = 600, Rank = 1 };
        var jsonResponse = JsonSerializer.Serialize(expectedBalance, UnbelievaBoatClient.DefaultJsonSerializerOptions);

        var client = CreateTestClient(MockHttpMessageHandler.Create(HttpStatusCode.OK, jsonResponse, req =>
        {
            req.Method.Should().Be(HttpMethod.Get);
            req.RequestUri.AbsolutePath.Should().Be($"{ApiPathPrefix}/guilds/{guildId}/users/{userId}");
        }));

        var result = await client.GetUserBalanceAsync(guildId, userId);

        result.Should().BeEquivalentTo(expectedBalance);
    }

    [Fact]
    public async Task SetUserBalanceAsync_ValidRequest_ReturnsUpdatedUserBalance()
    {
        var guildId = "g1";
        var userId = "u1";
        var requestPayload = new UpdateUserBalanceRequest { Cash = 1000, Bank = 2000, Reason = "Test set" };
        var expectedResponse = new UserBalance { UserId = userId, GuildId = guildId, Cash = 1000, Bank = 2000, Total = 3000 };
        var jsonResponse = JsonSerializer.Serialize(expectedResponse, UnbelievaBoatClient.DefaultJsonSerializerOptions);

        var client = CreateTestClient(MockHttpMessageHandler.Create(HttpStatusCode.OK, jsonResponse, async req =>
        {
            req.Method.Should().Be(HttpMethod.Put);
            req.RequestUri.AbsolutePath.Should().Be($"{ApiPathPrefix}/guilds/{guildId}/users/{userId}");
            var content = await req.Content.ReadAsStringAsync();
            var deserializedPayload = JsonSerializer.Deserialize<UpdateUserBalanceRequest>(content, UnbelievaBoatClient.DefaultJsonSerializerOptions);
            deserializedPayload.Should().BeEquivalentTo(requestPayload);
        }));

        var result = await client.SetUserBalanceAsync(guildId, userId, requestPayload);

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetGuildLeaderboardAsync_WithOptions_ConstructsCorrectUrl()
    {
        var guildId = "guild789";
        var options = new UnbelievaBoatClient.GuildLeaderboardOptions { Limit = 10, Offset = 5, Sort = "-total" };
        var expectedLeaderboard = new GuildLeaderboard { GuildId = guildId, Users = new List<UserBalance>() };
        var jsonResponse = JsonSerializer.Serialize(expectedLeaderboard, UnbelievaBoatClient.DefaultJsonSerializerOptions);

        string actualPathAndQuery = null;
        var client = CreateTestClient(MockHttpMessageHandler.Create(HttpStatusCode.OK, jsonResponse, req =>
        {
            req.Method.Should().Be(HttpMethod.Get);
            actualPathAndQuery = req.RequestUri.PathAndQuery; // Includes leading slash and query
        }));

        await client.GetGuildLeaderboardAsync(guildId, options);

        actualPathAndQuery.Should().StartWith($"{ApiPathPrefix}/guilds/{guildId}/users");
        actualPathAndQuery.Should().Contain("limit=10");
        actualPathAndQuery.Should().Contain("offset=5");
        actualPathAndQuery.Should().Contain("sort=-total");
    }

    public void Dispose()
    {
        _mockHttpClient?.Dispose();
        // _mockHttpMessageHandler does not need explicit dispose
    }
}
