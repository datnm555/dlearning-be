using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Api.IntegrationTests.Catalog;

public sealed class CatalogEndpointsTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record CategoryView(string Code, string Name, string IconKey, int DisplayOrder);
    private sealed record ProductView(string Code, string Name, string IconKey, int DisplayOrder, bool IsAvailable);

    [Fact]
    public async Task GetCategories_En_ReturnsEnglishNames()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryView>>("/categories?lang=en");

        categories.ShouldNotBeNull();
        categories.Count.ShouldBe(5);
        categories[0].Code.ShouldBe("preschool");
        categories[0].Name.ShouldBe("Preschool");
    }

    [Fact]
    public async Task GetCategories_Vi_ReturnsVietnameseNames()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryView>>("/categories?lang=vi");

        categories.ShouldNotBeNull();
        categories[0].Name.ShouldBe("Mầm non");
    }

    [Fact]
    public async Task GetCategories_NoLang_DefaultsToVietnamese()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryView>>("/categories");

        categories.ShouldNotBeNull();
        categories[0].Name.ShouldBe("Mầm non");
    }

    [Fact]
    public async Task GetProducts_Preschool_ReturnsFour_WithAlphabetAvailable()
    {
        var products = await _client.GetFromJsonAsync<List<ProductView>>("/categories/preschool/products?lang=en");

        products.ShouldNotBeNull();
        products.Count.ShouldBe(4);
        products[0].Code.ShouldBe("alphabet");
        products[0].Name.ShouldBe("Alphabet");
        products[0].IsAvailable.ShouldBeTrue();
        // All four preschool lessons are now available.
        products.Count(p => p.IsAvailable).ShouldBe(4);
        products.Single(p => p.Code == "colors").IsAvailable.ShouldBeTrue();
    }

    [Fact]
    public async Task GetProducts_EmptyCategory_ReturnsEmpty()
    {
        var response = await _client.GetAsync("/categories/university/products");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<ProductView>>();
        products.ShouldNotBeNull();
        products.ShouldBeEmpty();
    }
}
