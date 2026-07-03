using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Api.IntegrationTests.Colors;

public sealed class ColorsEndpointsTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record TokenOnly(string Token);
    private sealed record ColorView(string Code, string Name, string HexValue, string ExampleWord, string ExampleEmoji, int DisplayOrder);
    private sealed record ProductView(string Code, string Name, string IconKey, int DisplayOrder, bool IsAvailable);

    private async Task<string> LoginAsync()
    {
        var login = await _client.PostAsJsonAsync("/users/login",
            new { identifier = "demo", password = "Demo@123" });
        var auth = await login.Content.ReadFromJsonAsync<TokenOnly>();
        auth.ShouldNotBeNull();
        return auth.Token;
    }

    [Fact]
    public async Task GetColors_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/colors");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetColors_En_Returns11LocalizedColorsInOrder()
    {
        var token = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/colors?lang=en");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var colors = await response.Content.ReadFromJsonAsync<List<ColorView>>();
        colors.ShouldNotBeNull();
        colors.Count.ShouldBe(11);
        colors[0].Code.ShouldBe("red");
        colors[0].Name.ShouldBe("Red");
        colors[0].ExampleWord.ShouldBe("apple");
        colors[0].HexValue.ShouldBe("#EF4444");
        colors[^1].Code.ShouldBe("gray");
    }

    [Fact]
    public async Task GetColors_Vi_ReturnsVietnameseNames()
    {
        var token = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/colors?lang=vi");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        var colors = await response.Content.ReadFromJsonAsync<List<ColorView>>();
        colors.ShouldNotBeNull();
        colors[0].Name.ShouldBe("Đỏ");
        colors[0].ExampleWord.ShouldBe("quả táo");
    }

    [Fact]
    public async Task PreschoolProducts_ColorsIsNowAvailable()
    {
        var products = await _client.GetFromJsonAsync<List<ProductView>>("/categories/preschool/products?lang=en");

        products.ShouldNotBeNull();
        products.Single(p => p.Code == "colors").IsAvailable.ShouldBeTrue();
    }
}
