using Domain.Catalog;
using Shouldly;

namespace Application.UnitTests.Catalog;

public class CategoryTests
{
    [Fact]
    public void Category_And_Product_ExposeStructuralData()
    {
        var category = new Category { Code = "preschool", IconKey = "🧸", DisplayOrder = 1 };
        var product = new Product { CategoryId = category.Id, Code = "alphabet", IconKey = "🔤", DisplayOrder = 1, IsAvailable = true };

        category.Code.ShouldBe("preschool");
        category.DisplayOrder.ShouldBe(1);
        product.Code.ShouldBe("alphabet");
        product.IsAvailable.ShouldBeTrue();
    }
}
