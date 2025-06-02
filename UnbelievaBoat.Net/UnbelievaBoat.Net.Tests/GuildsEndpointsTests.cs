using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnbelievaBoat.Net.Models;
using Xunit;
using FluentAssertions;
using System.Text.Json;
using UnbelievaBoat.Net;

public class GuildsEndpointsTests : IDisposable
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
    public async Task GetGuildAsync_ValidRequest_ReturnsGuild()
    {
        var guildId = "guild123";
        var expectedGuild = new Guild
        {
            Id = guildId,
            Name = "Test Guild",
            Icon = "icon_hash",
            OwnerId = "owner456",
            MemberCount = 100,
            Symbol = "$",
            Roles = new System.Collections.Generic.List<GuildRole>() // Add sample roles if needed
        };
        var jsonResponse = JsonSerializer.Serialize(expectedGuild, UnbelievaBoatClient.DefaultJsonSerializerOptions);

        var client = CreateTestClient(MockHttpMessageHandler.Create(HttpStatusCode.OK, jsonResponse, req =>
        {
            req.Method.Should().Be(HttpMethod.Get);
            req.RequestUri.AbsolutePath.Should().Be($"{ApiPathPrefix}/guilds/{guildId}");
        }));

        var result = await client.GetGuildAsync(guildId);

        result.Should().BeEquivalentTo(expectedGuild);
    }

    public void Dispose()
    {
        _mockHttpClient?.Dispose();
    }
}
