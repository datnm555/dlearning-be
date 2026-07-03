using Application.Abstractions.Data;
using Application.Colors;
using Domain.Colors;
using MockQueryable.NSubstitute;
using NSubstitute;
using Shouldly;

namespace Application.UnitTests.Colors;

public class GetColorsQueryHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();

    [Fact]
    public async Task Handle_ReturnsColorsOrderedByDisplayOrder()
    {
        var colors = new List<Color>
        {
            new() { Code = "blue", HexValue = "#3B82F6", ExampleEmoji = "🫐", DisplayOrder = 5 },
            new() { Code = "red", HexValue = "#EF4444", ExampleEmoji = "🍎", DisplayOrder = 1 }
        };
        var set = colors.BuildMockDbSet();
        _dbContext.Colors.Returns(set);
        var handler = new GetColorsQueryHandler(_dbContext);

        var result = await handler.Handle(new GetColorsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].Code.ShouldBe("red");
        result.Value[1].Code.ShouldBe("blue");
    }
}
