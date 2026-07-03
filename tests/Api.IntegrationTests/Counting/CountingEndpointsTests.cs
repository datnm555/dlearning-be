using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Api.IntegrationTests.Counting;

public sealed class CountingEndpointsTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record TokenOnly(string Token);
    private sealed record NumberView(int Value, string Word, string Emoji);

    private async Task<string> LoginAsync()
    {
        var login = await _client.PostAsJsonAsync("/users/login",
            new { identifier = "demo", password = "Demo@123" });
        var auth = await login.Content.ReadFromJsonAsync<TokenOnly>();
        auth.ShouldNotBeNull();
        return auth.Token;
    }

    [Fact]
    public async Task GetCounting_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/counting");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCounting_Returns10NumbersInOrder_Localized()
    {
        var token = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/counting?lang=vi");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var numbers = await response.Content.ReadFromJsonAsync<List<NumberView>>();
        numbers.ShouldNotBeNull();
        numbers.Count.ShouldBe(10);
        numbers[0].Value.ShouldBe(1);
        numbers[0].Word.ShouldBe("Một");
        numbers[^1].Value.ShouldBe(10);
        numbers[^1].Word.ShouldBe("Mười");
    }
}
