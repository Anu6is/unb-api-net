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
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedInventory);
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

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResponseItem);
        capturedPayload.Should().BeEquivalentTo(requestPayload);
    }

    [Fact]
    public async Task GetUserInventoryItemAsync_WhenItemNotFound_ReturnsFailureResult()
    {
        var guildId = "g1";
        var userId = "u1";
        var inventoryItemId = "nonexistentInvItem";
        var errorJson = "{\"error\":\"Inventory item not found\"}";
        var expectedStatusCode = HttpStatusCode.NotFound;

        var client = CreateTestClient(MockHttpMessageHandler.Create(expectedStatusCode, errorJson, req =>
        {
            req.Method.Should().Be(HttpMethod.Get);
            req.RequestUri.AbsolutePath.Should().Be($"{ApiPathPrefix}/users/{userId}/guilds/{guildId}/inventory/{inventoryItemId}");
        }));

        var result = await client.GetUserInventoryItemAsync(guildId, userId, inventoryItemId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.StatusCode.Should().Be(expectedStatusCode);
        result.Error.RawContent.Should().Be(errorJson);
        result.Error.Message.Should().Contain($"API request failed: Not Found (Status: {expectedStatusCode}). Raw content: {errorJson}");
    }

    [Fact]
    public async Task RemoveUserInventoryItemAsync_WhenItemNotFound_ReturnsFailureResult()
    {
        var guildId = "g1";
        var userId = "u1";
        var storeItemId = "sItem1"; // This is the store item ID in the path
        var errorJson = "{\"error\":\"Item to remove not found in inventory or insufficient quantity\"}";
        var expectedStatusCode = HttpStatusCode.NotFound; // Or BadRequest, depending on API for this case

        var client = CreateTestClient(MockHttpMessageHandler.Create(expectedStatusCode, errorJson, req =>
        {
            req.Method.Should().Be(HttpMethod.Delete);
            req.RequestUri.AbsolutePath.Should().Be($"{ApiPathPrefix}/users/{userId}/guilds/{guildId}/inventory/{storeItemId}");
        }));

        var options = new UnbelievaBoatClient.RemoveUserInventoryItemOptions { Quantity = 1 };
        var result = await client.RemoveUserInventoryItemAsync(guildId, userId, storeItemId, options);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.StatusCode.Should().Be(expectedStatusCode);
        result.Error.RawContent.Should().Be(errorJson);
    }

    // TODO: Add success tests for GetUserInventoryItemAsync, RemoveUserInventoryItemAsync.

    public void Dispose() => _mockHttpClient?.Dispose();
}
