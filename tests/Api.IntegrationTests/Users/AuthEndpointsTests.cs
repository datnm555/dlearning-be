using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Api.IntegrationTests.Users;

public sealed class AuthEndpointsTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_WithSeededDemoAccount_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/users/login",
            new { identifier = "demo@dlearning.vn", password = "Demo@123" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginBody>();
        body.ShouldNotBeNull();
        body.Token.ShouldNotBeNullOrWhiteSpace();
        body.Username.ShouldBe("demo");
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/users/login",
            new { identifier = "demo", password = "nope" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_ThenLogin_Succeeds()
    {
        var register = await _client.PostAsJsonAsync("/users/register",
            new { email = "na@dlearning.vn", username = "bena", displayName = "Bé Na", password = "Secret123" });
        register.StatusCode.ShouldBe(HttpStatusCode.OK);

        var login = await _client.PostAsJsonAsync("/users/login",
            new { identifier = "bena", password = "Secret123" });
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var first = await _client.PostAsJsonAsync("/users/register",
            new { email = "dup@dlearning.vn", username = "dupuser1", displayName = "Dup", password = "Secret123" });
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        var second = await _client.PostAsJsonAsync("/users/register",
            new { email = "dup@dlearning.vn", username = "dupuser2", displayName = "Dup", password = "Secret123" });
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/users/register",
            new { email = "short@dlearning.vn", username = "shorty", displayName = "Short", password = "123" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private sealed record LoginBody(string Token, Guid UserId, string Email, string Username, string DisplayName);
}
