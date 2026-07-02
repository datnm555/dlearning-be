using Domain.Users;
using Shouldly;

namespace Application.UnitTests.Users;

public class UserTests
{
    [Fact]
    public void Create_WithValidInput_NormalizesEmailAndUsername_AndRaisesEvent()
    {
        var result = User.Create("  Demo@DLearning.VN ", " Demo ", "  Minh Anh ", "HASH");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Email.ShouldBe("demo@dlearning.vn");
        result.Value.Username.ShouldBe("demo");
        result.Value.DisplayName.ShouldBe("Minh Anh");
        result.Value.PasswordHash.ShouldBe("HASH");
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<UserRegisteredDomainEvent>();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void Create_WithInvalidEmail_ReturnsValidationFailure(string email)
    {
        var result = User.Create(email, "demo", "Minh Anh", "HASH");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.InvalidEmail");
    }

    [Theory]
    [InlineData("ab")]                 // too short
    [InlineData("has space")]          // non-alphanumeric
    public void Create_WithInvalidUsername_ReturnsValidationFailure(string username)
    {
        var result = User.Create("demo@dlearning.vn", username, "Minh Anh", "HASH");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.InvalidUsername");
    }

    [Fact]
    public void Create_WithBlankDisplayName_ReturnsValidationFailure()
    {
        var result = User.Create("demo@dlearning.vn", "demo", "   ", "HASH");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.InvalidDisplayName");
    }
}
