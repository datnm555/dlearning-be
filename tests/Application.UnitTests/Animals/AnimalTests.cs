using Domain.Animals;
using Domain.Counting;
using Shouldly;

namespace Application.UnitTests.Animals;

public class AnimalTests
{
    [Fact]
    public void Animal_And_CountingNumber_ExposeStructuralData()
    {
        var animal = new Animal { Code = "cat", Emoji = "🐱", DisplayOrder = 1 };
        var number = new CountingNumber { Value = 3, Emoji = "🐟" };

        animal.Code.ShouldBe("cat");
        animal.Emoji.ShouldBe("🐱");
        animal.DisplayOrder.ShouldBe(1);
        number.Value.ShouldBe(3);
        number.Emoji.ShouldBe("🐟");
    }
}
