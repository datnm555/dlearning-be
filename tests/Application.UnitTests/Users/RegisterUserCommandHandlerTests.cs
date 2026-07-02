using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Users;
using Domain.Users;
using MockQueryable.NSubstitute;
using NSubstitute;
using Shouldly;

namespace Application.UnitTests.Users;

public class RegisterUserCommandHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    private RegisterUserCommandHandler CreateHandler(List<User> users)
    {
        var usersDbSet = users.BuildMockDbSet();
        _dbContext.Users.Returns(usersDbSet);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("HASHED");
        return new RegisterUserCommandHandler(_dbContext, _passwordHasher);
    }

    [Fact]
    public async Task Handle_WithNewCredentials_HashesPassword_Saves_AndReturnsId()
    {
        var handler = CreateHandler([]);
        var command = new RegisterUserCommand("new@dlearning.vn", "newbie", "Bé Na", "Secret123");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
        _passwordHasher.Received(1).Hash("Secret123");
        _dbContext.Users.Received(1).Add(Arg.Is<User>(u => u.Email == "new@dlearning.vn" && u.PasswordHash == "HASHED"));
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithShortPassword_ReturnsValidation_AndDoesNotSave()
    {
        var handler = CreateHandler([]);
        var command = new RegisterUserCommand("new@dlearning.vn", "newbie", "Bé Na", "short");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.PasswordTooShort");
        await _dbContext.DidNotReceiveWithAnyArgs().SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ReturnsConflict()
    {
        var existing = User.Create("dup@dlearning.vn", "someone", "Ai Đó", "H").Value;
        var handler = CreateHandler([existing]);
        var command = new RegisterUserCommand("DUP@dlearning.vn", "brandnew", "Bé Na", "Secret123");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.EmailNotUnique");
    }

    [Fact]
    public async Task Handle_WithDuplicateUsername_ReturnsConflict()
    {
        var existing = User.Create("someone@dlearning.vn", "taken", "Ai Đó", "H").Value;
        var handler = CreateHandler([existing]);
        var command = new RegisterUserCommand("fresh@dlearning.vn", "TAKEN", "Bé Na", "Secret123");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.UsernameNotUnique");
    }
}
