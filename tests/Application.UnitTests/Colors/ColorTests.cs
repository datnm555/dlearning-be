using Domain.Colors;
using Shouldly;

namespace Application.UnitTests.Colors;

public class ColorTests
{
    [Fact]
    public void Color_ExposesStructuralData()
    {
        var color = new Color { Code = "red", HexValue = "#EF4444", ExampleEmoji = "🍎", DisplayOrder = 1 };

        color.Code.ShouldBe("red");
        color.HexValue.ShouldBe("#EF4444");
        color.ExampleEmoji.ShouldBe("🍎");
        color.DisplayOrder.ShouldBe(1);
    }
}
