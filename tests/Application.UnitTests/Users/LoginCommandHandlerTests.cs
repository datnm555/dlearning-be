using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Users;
using Domain.Users;
using MockQueryable.NSubstitute;
using NSubstitute;
using Shouldly;

namespace Application.UnitTests.Users;

public class LoginCommandHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenProvider _tokenProvider = Substitute.For<ITokenProvider>();

    private LoginCommandHandler CreateHandler(List<User> users)
    {
        var usersDbSet = users.BuildMockDbSet();
        _dbContext.Users.Returns(usersDbSet);
        return new LoginCommandHandler(_dbContext, _passwordHasher, _tokenProvider);
    }

    private static User SeededUser() =>
        User.Create("minhanh@dlearning.vn", "minhanh", "Minh Anh", "STORED_HASH").Value;

    [Fact]
    public async Task Handle_WithCorrectEmailAndPassword_ReturnsTokenAndProfile()
    {
        var user = SeededUser();
        var handler = CreateHandler([user]);
        _passwordHasher.Verify("pw", "STORED_HASH").Returns(true);
        _tokenProvider.Create(user).Returns("JWT");

        var result = await handler.Handle(new LoginCommand("minhanh@dlearning.vn", "pw"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Token.ShouldBe("JWT");
        result.Value.Username.ShouldBe("minhanh");
        result.Value.DisplayName.ShouldBe("Minh Anh");
    }

    [Fact]
    public async Task Handle_WithCorrectUsername_LogsIn()
    {
        var user = SeededUser();
        var handler = CreateHandler([user]);
        _passwordHasher.Verify("pw", "STORED_HASH").Returns(true);
        _tokenProvider.Create(user).Returns("JWT");

        var result = await handler.Handle(new LoginCommand("MINHANH", "pw"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Token.ShouldBe("JWT");
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ReturnsInvalidCredentials()
    {
        var user = SeededUser();
        var handler = CreateHandler([user]);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await handler.Handle(new LoginCommand("minhanh@dlearning.vn", "wrong"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.InvalidCredentials");
    }

    [Fact]
    public async Task Handle_WithUnknownUser_ReturnsInvalidCredentials()
    {
        var handler = CreateHandler([]);

        var result = await handler.Handle(new LoginCommand("ghost@dlearning.vn", "pw"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.InvalidCredentials");
    }
}
