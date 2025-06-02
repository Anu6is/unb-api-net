using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnbelievaBoat.Net.Models;
using Xunit;
using FluentAssertions;
using System.Text.Json;
using UnbelievaBoat.Net;
using System.Collections.Generic;

public class InventoryEndpointsTests : IDisposable
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
    public async Task GetUserInventoryItemsAsync_ValidRequest_ReturnsInventory()
    {
        var guildId = "g1";
        var userId = "u1";
        var expectedInventory = new List<InventoryItem>
        {
            new InventoryItem { Id = "invItem1", ItemId = "storeItem1", Name = "Sword", Quantity = 1 }
        };
        var jsonResponse = JsonSerializer.Serialize(expectedInventory, UnbelievaBoatClient.DefaultJsonSerializerOptions);

        var client = CreateTestClient(MockHttpMessageHandler.Create(HttpStatusCode.OK, jsonResponse, req =>
        {
            req.Method.Should().Be(HttpMethod.Get);
            req.RequestUri.AbsolutePath.Should().Be($"{ApiPathPrefix}/users/{userId}/guilds/{guildId}/inventory");
        }));

        var result = await client.GetUserInventoryItemsAsync(guildId, userId);
        result.Should().BeEquivalentTo(expectedInventory);
    }

    [Fact]
    public async Task AddUserInventoryItemAsync_ValidRequest_ReturnsUpdatedInventoryItem()
    {
        var guildId = "g1";
        var userId = "u1";
        var storeItemId = "sItem1";
        var requestPayload = new AddInventoryItemRequest { Quantity = 2 };
        var expectedResponseItem = new InventoryItem { Id = "invInstance1", ItemId = storeItemId, Name = "Shield", Quantity = 2 };
        var jsonResponse = JsonSerializer.Serialize(expectedResponseItem, UnbelievaBoatClient.DefaultJsonSerializerOptions);

        AddInventoryItemRequest capturedPayload = null;

        var client = CreateTestClient(MockHttpMessageHandler.Create(HttpStatusCode.OK, jsonResponse, async req =>
        {
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri.AbsolutePath.Should().Be($"{ApiPathPrefix}/users/{userId}/guilds/{guildId}/inventory/{storeItemId}");
            var content = await req.Content.ReadAsStringAsync();
            capturedPayload = JsonSerializer.Deserialize<AddInventoryItemRequest>(content, UnbelievaBoatClient.DefaultJsonSerializerOptions);
        }));

        var result = await client.AddUserInventoryItemAsync(guildId, userId, storeItemId, requestPayload);

        result.Should().BeEquivalentTo(expectedResponseItem);
        capturedPayload.Should().BeEquivalentTo(requestPayload);
    }

    // TODO: Add tests for GetUserInventoryItemAsync, RemoveUserInventoryItemAsync.
    // Test RemoveUserInventoryItemAsync with the SendDeleteRequestWithResponseAsync helper.

    public void Dispose() => _mockHttpClient?.Dispose();
}
