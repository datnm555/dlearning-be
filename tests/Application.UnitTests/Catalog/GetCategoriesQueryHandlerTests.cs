using Application.Abstractions.Data;
using Application.Catalog;
using Domain.Catalog;
using MockQueryable.NSubstitute;
using NSubstitute;
using Shouldly;

namespace Application.UnitTests.Catalog;

public class GetCategoriesQueryHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();

    [Fact]
    public async Task Handle_ReturnsCategoriesOrderedByDisplayOrder()
    {
        var categories = new List<Category>
        {
            new() { Code = "primary", IconKey = "✏️", DisplayOrder = 2 },
            new() { Code = "preschool", IconKey = "🧸", DisplayOrder = 1 }
        };
        var set = categories.BuildMockDbSet();
        _dbContext.Categories.Returns(set);
        var handler = new GetCategoriesQueryHandler(_dbContext);

        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].Code.ShouldBe("preschool");
        result.Value[1].Code.ShouldBe("primary");
    }
}
