using Application.Abstractions.Data;
using Application.Counting;
using Domain.Counting;
using MockQueryable.NSubstitute;
using NSubstitute;
using Shouldly;

namespace Application.UnitTests.Counting;

public class GetCountingQueryHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();

    [Fact]
    public async Task Handle_ReturnsNumbersOrderedByValue()
    {
        var numbers = new List<CountingNumber>
        {
            new() { Value = 2, Emoji = "🍌" },
            new() { Value = 1, Emoji = "🍎" }
        };
        var set = numbers.BuildMockDbSet();
        _dbContext.CountingNumbers.Returns(set);
        var handler = new GetCountingQueryHandler(_dbContext);

        var result = await handler.Handle(new GetCountingQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].Value.ShouldBe(1);
        result.Value[1].Value.ShouldBe(2);
    }
}
