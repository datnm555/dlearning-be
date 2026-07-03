using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Api.IntegrationTests.Animals;

public sealed class AnimalsEndpointsTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record TokenOnly(string Token);
    private sealed record AnimalView(string Code, string Name, string Emoji, string Sound, int DisplayOrder);

    private async Task<string> LoginAsync()
    {
        var login = await _client.PostAsJsonAsync("/users/login",
            new { identifier = "demo", password = "Demo@123" });
        var auth = await login.Content.ReadFromJsonAsync<TokenOnly>();
        auth.ShouldNotBeNull();
        return auth.Token;
    }

    [Fact]
    public async Task GetAnimals_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/animals");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAnimals_En_Returns12LocalizedAnimals()
    {
        var token = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/animals?lang=en");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var animals = await response.Content.ReadFromJsonAsync<List<AnimalView>>();
        animals.ShouldNotBeNull();
        animals.Count.ShouldBe(12);
        animals[0].Code.ShouldBe("cat");
        animals[0].Name.ShouldBe("Cat");
        animals[0].Sound.ShouldBe("meow");
        animals[^1].Code.ShouldBe("frog");
    }

    [Fact]
    public async Task GetAnimals_Vi_ReturnsVietnameseNamesAndSounds()
    {
        var token = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/animals?lang=vi");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        var animals = await response.Content.ReadFromJsonAsync<List<AnimalView>>();
        animals.ShouldNotBeNull();
        animals[0].Name.ShouldBe("Mèo");
        animals[0].Sound.ShouldBe("meo meo");
    }
}
