using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Api.IntegrationTests.Alphabets;

public sealed class AlphabetEndpointsTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAlphabet_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/alphabet");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAlphabet_WithToken_Returns29LettersInOrder()
    {
        var login = await _client.PostAsJsonAsync("/users/login",
            new { identifier = "demo", password = "Demo@123" });
        var auth = await login.Content.ReadFromJsonAsync<TokenOnly>();
        auth.ShouldNotBeNull();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/alphabet");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var letters = await response.Content.ReadFromJsonAsync<List<LetterBody>>();
        letters.ShouldNotBeNull();
        letters.Count.ShouldBe(29);
        letters[0].UpperCase.ShouldBe("A");
        letters[3].UpperCase.ShouldBe("B");
        letters[^1].UpperCase.ShouldBe("Y");
    }

    private sealed record TokenOnly(string Token);
    private sealed record LetterBody(Guid Id, string UpperCase, string LowerCase, string Name, string Sound, string ExampleWord, string ExampleEmoji, int DisplayOrder);
}
