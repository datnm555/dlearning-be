using Application.Abstractions.Data;
using Application.Alphabets;
using Domain.Alphabets;
using MockQueryable.NSubstitute;
using NSubstitute;
using Shouldly;

namespace Application.UnitTests.Alphabets;

public class GetAlphabetQueryHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();

    [Fact]
    public async Task Handle_ReturnsLettersOrderedByDisplayOrder()
    {
        var letters = new List<AlphabetLetter>
        {
            new() { UpperCase = "B", LowerCase = "b", Name = "bê", Sound = "bờ", ExampleWord = "bò", ExampleEmoji = "🐄", DisplayOrder = 4 },
            new() { UpperCase = "A", LowerCase = "a", Name = "a", Sound = "a", ExampleWord = "áo", ExampleEmoji = "👕", DisplayOrder = 1 }
        };
        var lettersDbSet = letters.BuildMockDbSet();
        _dbContext.AlphabetLetters.Returns(lettersDbSet);
        var handler = new GetAlphabetQueryHandler(_dbContext);

        var result = await handler.Handle(new GetAlphabetQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].UpperCase.ShouldBe("A");
        result.Value[1].UpperCase.ShouldBe("B");
    }
}
