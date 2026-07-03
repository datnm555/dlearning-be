using Application.Abstractions.Data;
using Application.Animals;
using Domain.Animals;
using MockQueryable.NSubstitute;
using NSubstitute;
using Shouldly;

namespace Application.UnitTests.Animals;

public class GetAnimalsQueryHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();

    [Fact]
    public async Task Handle_ReturnsAnimalsOrderedByDisplayOrder()
    {
        var animals = new List<Animal>
        {
            new() { Code = "dog", Emoji = "🐶", DisplayOrder = 2 },
            new() { Code = "cat", Emoji = "🐱", DisplayOrder = 1 }
        };
        var set = animals.BuildMockDbSet();
        _dbContext.Animals.Returns(set);
        var handler = new GetAnimalsQueryHandler(_dbContext);

        var result = await handler.Handle(new GetAnimalsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].Code.ShouldBe("cat");
        result.Value[1].Code.ShouldBe("dog");
    }
}
