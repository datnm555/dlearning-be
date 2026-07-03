using Application.Abstractions.Data;
using Application.Catalog;
using Domain.Catalog;
using MockQueryable.NSubstitute;
using NSubstitute;
using Shouldly;

namespace Application.UnitTests.Catalog;

public class GetProductsByCategoryQueryHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();

    private GetProductsByCategoryQueryHandler CreateHandler()
    {
        var categories = new List<Category>
        {
            new() { Code = "preschool", IconKey = "🧸", DisplayOrder = 1 } // Id defaults to Guid.Empty
        };
        var products = new List<Product>
        {
            new() { CategoryId = Guid.Empty, Code = "animals", IconKey = "🐾", DisplayOrder = 2, IsAvailable = false },
            new() { CategoryId = Guid.Empty, Code = "alphabet", IconKey = "🔤", DisplayOrder = 1, IsAvailable = true },
            new() { CategoryId = new Guid("ca7e0000-0000-4000-8000-0000000000ff"), Code = "other", IconKey = "x", DisplayOrder = 1, IsAvailable = false }
        };
        var catSet = categories.BuildMockDbSet();
        var prodSet = products.BuildMockDbSet();
        _dbContext.Categories.Returns(catSet);
        _dbContext.Products.Returns(prodSet);
        return new GetProductsByCategoryQueryHandler(_dbContext);
    }

    [Fact]
    public async Task Handle_ReturnsProductsForCategory_OrderedAndFiltered()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new GetProductsByCategoryQuery("preschool"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].Code.ShouldBe("alphabet");
        result.Value[0].IsAvailable.ShouldBeTrue();
        result.Value[1].Code.ShouldBe("animals");
    }

    [Fact]
    public async Task Handle_UnknownCategory_ReturnsEmpty()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new GetProductsByCategoryQuery("nope"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }
}
