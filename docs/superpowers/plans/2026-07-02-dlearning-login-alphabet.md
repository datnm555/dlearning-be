# dLearning — Login & Vietnamese Alphabet Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a login/register API + a Vietnamese-alphabet learning API (.NET 10 clean architecture, PostgreSQL in Docker) and a React+TypeScript web app whose login screen matches the provided design and whose home screen teaches the 29-letter Vietnamese alphabet.

**Architecture:** Backend copies the `clean-architect-template` skeleton (SharedKernel → Domain → Application → Infrastructure → Web.Api, dependency rule enforced by NetArchTest), drops the `Examples` sample slice, and adds two vertical slices: `Users` (register/login, PBKDF2 password hashing, JWT) and `Alphabets` (read-only reference data seeded with 29 letters). Frontend is a Vite React-TS app with its own clean-architecture layering (domain / application / infrastructure / presentation / di); presentation talks only to use-cases, use-cases talk only to repository ports, and the `di` container is the single place that wires HTTP adapters to ports.

**Tech Stack:** .NET 10, EF Core 10 + Npgsql, native PBKDF2 (`System.Security.Cryptography.Rfc2898DeriveBytes` — no third-party crypto package), `Microsoft.AspNetCore.Authentication.JwtBearer` + `System.IdentityModel.Tokens.Jwt`, xUnit/Shouldly/NSubstitute/MockQueryable/Testcontainers. Frontend: Vite, React 18, TypeScript, react-router-dom, Vitest + Testing Library.

## Global Constraints

- **.NET pinned to 10** via `global.json` (`rollForward: latestMinor`) — do not change.
- **Central Package Management** — every NuGet version lives in `Directory.Packages.props`; never put `Version=` on a `<PackageReference>` in a `.csproj`.
- **`TreatWarningsAsErrors=true` + `EnforceCodeStyleInBuild=true`** (from `Directory.Build.props`) — any analyzer or `.editorconfig` style violation fails the build. File-scoped namespaces, braces always, `var` only when the RHS type is apparent, `I`-prefixed interfaces, 4-space indent for `.cs` / 2-space for `.json`/`.csproj`/`.props`/`.slnx`.
- **Dependency rule** `SharedKernel → Domain → Application → Infrastructure → Web.Api`, enforced by `tests/ArchitectureTests/Layers/LayerTests.cs`. Application may reference `Microsoft.EntityFrameworkCore` but NOT `Microsoft.AspNetCore`/`Microsoft.Extensions.Hosting`. Infrastructure must not reference `Web.Api` or ASP.NET.
- **Handler convention** (enforced by ArchitectureTests): types ending `CommandHandler`/`QueryHandler` must be `internal sealed` and implement `ICommandHandler<>`/`ICommandHandler<,>`/`IQueryHandler<,>`.
- **Feature-folder convention**: feature folders plural (`Users/`, `Alphabets/`), aggregate singular (`User`, `AlphabetLetter`).
- **Result pattern**: use cases return `Result`/`Result<T>`; failures carry an `Error` whose `ErrorType` maps to an HTTP status in `Web.Api/Middleware/ResultExtensions.cs`. Endpoints end with `result.ToHttpResult(...)`.
- **Backend repo**: `/Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be` (git repo, branch `main`). **Frontend repo**: `/Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-web` (git repo). Commit in the repo the changed files belong to.
- **Password hashing**: native PBKDF2-HMAC-SHA256, 600,000 iterations, 128-bit random salt, 256-bit key, constant-time compare. Stored format: `pbkdf2-sha256$<iterations>$<base64 salt>$<base64 key>` (self-describing — verifier reads iterations+salt from the string). No third-party package; `Microsoft.AspNetCore.Identity.PasswordHasher` is deliberately avoided because its namespace `Microsoft.AspNetCore.*` would fail the `Infrastructure_Should_NotDependOn_WebFrameworks` architecture test.
- **Demo account** (seeded): email `demo@dlearning.vn`, username `demo`, password `Demo@123`, PBKDF2 hash `pbkdf2-sha256$600000$zmz5Jrdt8HVqMz3413OHZQ==$6f+Gd25WENriwukIIkMp+VFRVxMvbzPNjZ+11YdJXPw=`.
- **JWT dev config** (committed to `appsettings.json`, learning project — not production): Secret `dlearning-super-secret-signing-key-change-me-1234567890`, Issuer `dlearning`, Audience `dlearning-web`, ExpirationInMinutes `60`.
- **29 Vietnamese letters** with static seed GUIDs of the form `d1ea0000-0000-4000-8000-0000000000NN` where `NN` is the display order in hex (`01`..`1d`). Demo user GUID: `d1ea0000-0000-4000-8000-000000000100`.

---

# Phase A — Backend (`dlearning-be`)

## Task A1: Scaffold the solution from the template (strip `Examples`)

**Files:**
- Copy all of `clean-architect-template/{src,tests,.config,.editorconfig,.gitignore,Directory.Build.props,Directory.Packages.props,global.json,NuGet.config}` into `dlearning-be/` (do NOT copy `.git`, `.idea`, `.claude`, `LICENSE`, `README.md`, `CLAUDE.md`, `plan.md`, `docs/`, `.DS_Store`, `CleanArchitect.slnx`).
- Create: `dlearning-be/DLearning.slnx`
- Delete after copy: `src/Domain/Examples/`, `src/Application/Examples/`, `src/Infrastructure/Examples/`, `src/Web.Api/Endpoints/Examples/`, `src/Infrastructure/Database/Migrations/` (all files), `tests/Application.UnitTests/Examples/`, `tests/Api.IntegrationTests/Examples/`
- Modify: `src/Application/Abstractions/Data/IApplicationDbContext.cs`, `src/Infrastructure/Database/ApplicationDbContext.cs`, `src/Application/DependencyInjection.cs`, `src/Infrastructure/DependencyInjection.cs`, `tests/ArchitectureTests/BaseTest.cs`

- [ ] **Step 1: Copy template into the repo**

```bash
SRC=/Users/dat.nguyenmanh/Desktop/dat/my-git/clean-architect-template
DST=/Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be
rsync -a --exclude='.git' --exclude='.idea' --exclude='.claude' --exclude='obj' --exclude='bin' \
  --exclude='LICENSE' --exclude='README.md' --exclude='CLAUDE.md' --exclude='plan.md' \
  --exclude='docs' --exclude='.DS_Store' --exclude='CleanArchitect.slnx' \
  "$SRC"/ "$DST"/
```

- [ ] **Step 2: Delete the `Examples` slice and template migrations**

```bash
DST=/Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be
rm -rf "$DST"/src/Domain/Examples \
       "$DST"/src/Application/Examples \
       "$DST"/src/Infrastructure/Examples \
       "$DST"/src/Web.Api/Endpoints/Examples \
       "$DST"/src/Infrastructure/Database/Migrations \
       "$DST"/tests/Application.UnitTests/Examples \
       "$DST"/tests/Api.IntegrationTests/Examples
```

- [ ] **Step 3: Create `DLearning.slnx`**

Create `dlearning-be/DLearning.slnx`:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/Application/Application.csproj" />
    <Project Path="src/Domain/Domain.csproj" />
    <Project Path="src/Infrastructure/Infrastructure.csproj" />
    <Project Path="src/SharedKernel/SharedKernel.csproj" />
    <Project Path="src/Web.Api/Web.Api.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/Api.IntegrationTests/Api.IntegrationTests.csproj" />
    <Project Path="tests/Application.UnitTests/Application.UnitTests.csproj" />
    <Project Path="tests/ArchitectureTests/ArchitectureTests.csproj" />
  </Folder>
</Solution>
```

- [ ] **Step 4: Strip `Examples` references so it compiles**

Replace `src/Application/Abstractions/Data/IApplicationDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<T> Set<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

Replace `src/Infrastructure/Database/ApplicationDbContext.cs`:

```csharp
using Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

Replace `src/Application/DependencyInjection.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
```

Replace `src/Infrastructure/DependencyInjection.cs`:

```csharp
using Application.Abstractions.Data;
using Infrastructure.Database;
using Infrastructure.Database.Interceptors;
using Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<AuditingInterceptor>();

        string connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is missing.");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }
}
```

Replace `tests/ArchitectureTests/BaseTest.cs` (DomainAssembly now anchors on `SystemConstants`):

```csharp
using System.Reflection;
using Application.Abstractions.Messaging;
using Domain;
using Infrastructure;
using SharedKernel;

namespace ArchitectureTests;

/// <summary>
/// Holds Assembly references for every layer the architecture tests inspect.
/// </summary>
public abstract class BaseTest
{
    protected static readonly Assembly SharedKernelAssembly = typeof(Entity).Assembly;
    protected static readonly Assembly DomainAssembly = typeof(SystemConstants).Assembly;
    protected static readonly Assembly ApplicationAssembly = typeof(ICommand).Assembly;
    protected static readonly Assembly InfrastructureAssembly = typeof(DependencyInjection).Assembly;
}
```

- [ ] **Step 5: Rename the DB name in `appsettings.json`**

In `src/Web.Api/appsettings.json` change the connection string database to `dlearning`:

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5432;Database=dlearning;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 6: Build and run the (now feature-less) skeleton tests**

Run: `cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be && dotnet build DLearning.slnx`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

Run: `dotnet test tests/ArchitectureTests/`
Expected: PASS (all layer/handler-convention tests green; no handlers exist yet so handler tests pass vacuously).

- [ ] **Step 7: Commit**

```bash
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be
git add -A
git commit -m "chore: scaffold dLearning backend from clean-architecture template

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A2: PostgreSQL via Docker Compose

**Files:**
- Create: `dlearning-be/docker-compose.yml`
- Create: `dlearning-be/.dockerignore` (optional — skip)

- [ ] **Step 1: Write `docker-compose.yml`**

Create `dlearning-be/docker-compose.yml`:

```yaml
services:
  postgres:
    image: postgres:17-alpine
    container_name: dlearning-postgres
    environment:
      POSTGRES_DB: dlearning
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - dlearning_pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d dlearning"]
      interval: 5s
      timeout: 5s
      retries: 5

volumes:
  dlearning_pgdata:
```

- [ ] **Step 2: Start the database and verify it is healthy**

Run: `cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be && docker compose up -d`
Expected: container `dlearning-postgres` created and started.

Run: `docker compose ps`
Expected: `dlearning-postgres` status shows `running` / `healthy`.

Run: `docker exec dlearning-postgres pg_isready -U postgres -d dlearning`
Expected: `/var/run/postgresql:5432 - accepting connections`

- [ ] **Step 3: Commit**

```bash
git add docker-compose.yml
git commit -m "chore: add PostgreSQL docker-compose for local dev

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A3: Add packages + `ErrorType.Unauthorized` + 401 mapping

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `src/Domain/Domain.csproj` (none), `src/Infrastructure/Infrastructure.csproj`, `src/Web.Api/Web.Api.csproj`
- Modify: `src/SharedKernel/ErrorType.cs`, `src/SharedKernel/Error.cs`
- Modify: `src/Web.Api/Middleware/ResultExtensions.cs`

**Interfaces:**
- Produces: `ErrorType.Unauthorized` enum member; `Error.Unauthorized(string code, string description)` factory; `ResultExtensions.MapError` maps `Unauthorized → 401`.

- [ ] **Step 1: Add package versions**

In `Directory.Packages.props`, add inside `<ItemGroup>`:

```xml
    <!-- Auth (password hashing uses the BCL — no crypto package needed) -->
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.7" />
    <PackageVersion Include="System.IdentityModel.Tokens.Jwt" Version="8.14.0" />
```

> If `dotnet restore` reports that `System.IdentityModel.Tokens.Jwt 8.14.0` is not found, run `dotnet package search System.IdentityModel.Tokens.Jwt --take 5` and pin the latest available **8.x** (that major line is what `Microsoft.AspNetCore.Authentication.JwtBearer 10.x` depends on, so staying on 8.x avoids a `Microsoft.IdentityModel.*` version conflict).

- [ ] **Step 2: Reference packages from the projects that need them**

In `src/Infrastructure/Infrastructure.csproj`, add to the first `<ItemGroup>` (package references):

```xml
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" />
```

In `src/Web.Api/Web.Api.csproj`, add a package `<ItemGroup>`:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
  </ItemGroup>
```

- [ ] **Step 3: Add `Unauthorized` to the error model**

Replace `src/SharedKernel/ErrorType.cs`:

```csharp
namespace SharedKernel;

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    Problem = 2,
    NotFound = 3,
    Conflict = 4,
    Unauthorized = 5
}
```

In `src/SharedKernel/Error.cs`, add the factory below the existing `Conflict` factory:

```csharp
    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);
```

- [ ] **Step 4: Map `Unauthorized → 401`**

In `src/Web.Api/Middleware/ResultExtensions.cs`, add a case to the `MapError` switch (above the `Conflict` line is fine):

```csharp
        ErrorType.Unauthorized => Results.Problem(
            detail: error.Description,
            title: error.Code,
            statusCode: StatusCodes.Status401Unauthorized),
```

- [ ] **Step 5: Build**

Run: `dotnet build DLearning.slnx`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add Directory.Packages.props src/Infrastructure/Infrastructure.csproj src/Web.Api/Web.Api.csproj src/SharedKernel/ src/Web.Api/Middleware/ResultExtensions.cs
git commit -m "feat: add auth packages and Unauthorized error type mapping to 401

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A4: `User` aggregate (Domain)

**Files:**
- Create: `src/Domain/Users/User.cs`
- Create: `src/Domain/Users/UserErrors.cs`
- Create: `src/Domain/Users/UserRegisteredDomainEvent.cs`
- Test: `tests/Application.UnitTests/Users/UserTests.cs`

**Interfaces:**
- Produces: `User.Create(string email, string username, string displayName, string passwordHash) : Result<User>` (normalizes email+username to trimmed lowercase, trims displayName, raises `UserRegisteredDomainEvent`); public read-only props `Email`, `Username`, `DisplayName`, `PasswordHash`. `UserErrors.EmailNotUnique`, `.UsernameNotUnique`, `.InvalidCredentials` (Unauthorized), `.InvalidEmail`, `.InvalidUsername`, `.InvalidDisplayName` (all Validation).

- [ ] **Step 1: Write the failing test**

Create `tests/Application.UnitTests/Users/UserTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~UserTests"`
Expected: FAIL (build error — `Domain.Users.User` does not exist).

- [ ] **Step 3: Write the domain event**

Create `src/Domain/Users/UserRegisteredDomainEvent.cs`:

```csharp
using SharedKernel;

namespace Domain.Users;

public sealed record UserRegisteredDomainEvent(Guid UserId, string Email) : IDomainEvent;
```

- [ ] **Step 4: Write the errors**

Create `src/Domain/Users/UserErrors.cs`:

```csharp
using SharedKernel;

namespace Domain.Users;

public static class UserErrors
{
    public static readonly Error InvalidEmail = Error.Validation(
        "Users.InvalidEmail",
        "Email không hợp lệ.");

    public static readonly Error InvalidUsername = Error.Validation(
        "Users.InvalidUsername",
        "Tên đăng nhập phải dài 3–30 ký tự và chỉ gồm chữ cái hoặc chữ số.");

    public static readonly Error InvalidDisplayName = Error.Validation(
        "Users.InvalidDisplayName",
        "Tên hiển thị không được để trống.");

    public static readonly Error PasswordTooShort = Error.Validation(
        "Users.PasswordTooShort",
        "Mật khẩu phải có ít nhất 8 ký tự.");

    public static readonly Error EmailNotUnique = Error.Conflict(
        "Users.EmailNotUnique",
        "Email này đã được đăng ký.");

    public static readonly Error UsernameNotUnique = Error.Conflict(
        "Users.UsernameNotUnique",
        "Tên đăng nhập này đã tồn tại.");

    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "Users.InvalidCredentials",
        "Thông tin đăng nhập không đúng.");
}
```

- [ ] **Step 5: Write the aggregate**

Create `src/Domain/Users/User.cs`:

```csharp
using System.Text.RegularExpressions;
using SharedKernel;

namespace Domain.Users;

public sealed partial class User : AggregateRoot
{
    private User() { }

    public string Email { get; private set; } = string.Empty;

    public string Username { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public static Result<User> Create(
        string email,
        string username,
        string displayName,
        string passwordHash)
    {
        string normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedEmail.Length is 0 or > 256 || !EmailRegex().IsMatch(normalizedEmail))
        {
            return Result.Failure<User>(UserErrors.InvalidEmail);
        }

        string normalizedUsername = (username ?? string.Empty).Trim().ToLowerInvariant();
        if (!UsernameRegex().IsMatch(normalizedUsername))
        {
            return Result.Failure<User>(UserErrors.InvalidUsername);
        }

        string trimmedDisplayName = (displayName ?? string.Empty).Trim();
        if (trimmedDisplayName.Length is 0 or > 100)
        {
            return Result.Failure<User>(UserErrors.InvalidDisplayName);
        }

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = normalizedEmail,
            Username = normalizedUsername,
            DisplayName = trimmedDisplayName,
            PasswordHash = passwordHash
        };

        user.Raise(new UserRegisteredDomainEvent(user.Id, user.Email));

        return user;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex("^[a-z0-9]{3,30}$")]
    private static partial Regex UsernameRegex();
}
```

- [ ] **Step 6: Suppress CA1308 (we deliberately lowercase emails/usernames)**

`ToLowerInvariant()` trips analyzer **CA1308** ("prefer normalizing to uppercase"), and with `TreatWarningsAsErrors` that fails the build. Lowercasing is the correct normalization for case-insensitive email/username identifiers, and the *invariant* variant is culture-safe. Append this to `.editorconfig` in the same `# Reason:` style as the existing entries:

```ini
# CA1308: Normalize strings to uppercase
# Reason: Emails and usernames are stored lowercase on purpose (case-insensitive identifiers).
#         ToLowerInvariant() is culture-safe; uppercasing them would be wrong for this domain.
dotnet_diagnostic.CA1308.severity = none
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~UserTests"`
Expected: PASS (all UserTests green).

- [ ] **Step 8: Commit**

```bash
git add src/Domain/Users tests/Application.UnitTests/Users .editorconfig
git commit -m "feat: add User aggregate with validation and registration event

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A5: Auth ports + register `DbSet<User>` on the context

**Files:**
- Create: `src/Application/Abstractions/Authentication/IPasswordHasher.cs`
- Create: `src/Application/Abstractions/Authentication/ITokenProvider.cs`
- Modify: `src/Application/Abstractions/Data/IApplicationDbContext.cs`
- Modify: `src/Infrastructure/Database/ApplicationDbContext.cs`

**Interfaces:**
- Produces: `IPasswordHasher { string Hash(string password); bool Verify(string password, string passwordHash); }`; `ITokenProvider { string Create(User user); }`; `IApplicationDbContext.Users : DbSet<User>`.

- [ ] **Step 1: Write the password-hasher port**

Create `src/Application/Abstractions/Authentication/IPasswordHasher.cs`:

```csharp
namespace Application.Abstractions.Authentication;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
```

- [ ] **Step 2: Write the token-provider port**

Create `src/Application/Abstractions/Authentication/ITokenProvider.cs`:

```csharp
using Domain.Users;

namespace Application.Abstractions.Authentication;

public interface ITokenProvider
{
    string Create(User user);
}
```

- [ ] **Step 3: Expose `Users` on the DbContext port**

Replace `src/Application/Abstractions/Data/IApplicationDbContext.cs`:

```csharp
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<T> Set<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

Replace `src/Infrastructure/Database/ApplicationDbContext.cs`:

```csharp
using Application.Abstractions.Data;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

- [ ] **Step 4: Build**

Run: `dotnet build DLearning.slnx`
Expected: `Build succeeded.` (EF will warn at runtime that `User` has no configuration yet, but it compiles; configuration comes in Task A9.)

- [ ] **Step 5: Commit**

```bash
git add src/Application/Abstractions src/Infrastructure/Database/ApplicationDbContext.cs
git commit -m "feat: add password-hasher and token-provider ports, expose Users DbSet

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A6: `RegisterUserCommand` + handler

**Files:**
- Create: `src/Application/Users/RegisterUserCommand.cs`
- Create: `src/Application/Users/RegisterUserCommandHandler.cs`
- Modify: `src/Application/DependencyInjection.cs`
- Test: `tests/Application.UnitTests/Users/RegisterUserCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IApplicationDbContext.Users`, `IPasswordHasher`, `User.Create`, `UserErrors`.
- Produces: `RegisterUserCommand(string Email, string Username, string DisplayName, string Password) : ICommand<Guid>`; handler returns `Result<Guid>` (new user id).

- [ ] **Step 1: Write the failing test**

Create `tests/Application.UnitTests/Users/RegisterUserCommandHandlerTests.cs`:

```csharp
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
        _dbContext.Users.Returns(users.BuildMockDbSet());
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~RegisterUserCommandHandlerTests"`
Expected: FAIL (build error — `RegisterUserCommand` does not exist).

- [ ] **Step 3: Write the command**

Create `src/Application/Users/RegisterUserCommand.cs`:

```csharp
using Application.Abstractions.Messaging;

namespace Application.Users;

public sealed record RegisterUserCommand(
    string Email,
    string Username,
    string DisplayName,
    string Password) : ICommand<Guid>;
```

- [ ] **Step 4: Write the handler**

Create `src/Application/Users/RegisterUserCommandHandler.cs`:

```csharp
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users;

internal sealed class RegisterUserCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        if ((command.Password ?? string.Empty).Length < 8)
        {
            return Result.Failure<Guid>(UserErrors.PasswordTooShort);
        }

        string email = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
        string username = (command.Username ?? string.Empty).Trim().ToLowerInvariant();

        if (await dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return Result.Failure<Guid>(UserErrors.EmailNotUnique);
        }

        if (await dbContext.Users.AnyAsync(u => u.Username == username, cancellationToken))
        {
            return Result.Failure<Guid>(UserErrors.UsernameNotUnique);
        }

        string passwordHash = passwordHasher.Hash(command.Password!);

        Result<User> userResult = User.Create(command.Email!, command.Username!, command.DisplayName!, passwordHash);
        if (userResult.IsFailure)
        {
            return Result.Failure<Guid>(userResult.Error);
        }

        dbContext.Users.Add(userResult.Value);
        await dbContext.SaveChangesAsync(cancellationToken);

        return userResult.Value.Id;
    }
}
```

- [ ] **Step 5: Register the handler**

Replace `src/Application/DependencyInjection.cs`:

```csharp
using Application.Abstractions.Messaging;
using Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<RegisterUserCommand, Guid>, RegisterUserCommandHandler>();
        return services;
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~RegisterUserCommandHandlerTests"`
Expected: PASS (4 tests green).

- [ ] **Step 7: Commit**

```bash
git add src/Application/Users src/Application/DependencyInjection.cs tests/Application.UnitTests/Users/RegisterUserCommandHandlerTests.cs
git commit -m "feat: add RegisterUser command, handler, and unit tests

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A7: `LoginCommand` + handler

**Files:**
- Create: `src/Application/Users/LoginCommand.cs`
- Create: `src/Application/Users/Data/LoginResponse.cs`
- Create: `src/Application/Users/LoginCommandHandler.cs`
- Modify: `src/Application/DependencyInjection.cs`
- Test: `tests/Application.UnitTests/Users/LoginCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IApplicationDbContext.Users`, `IPasswordHasher.Verify`, `ITokenProvider.Create`, `UserErrors.InvalidCredentials`.
- Produces: `LoginCommand(string Identifier, string Password) : ICommand<LoginResponse>`; `LoginResponse(string Token, Guid UserId, string Email, string Username, string DisplayName)`.

- [ ] **Step 1: Write the failing test**

Create `tests/Application.UnitTests/Users/LoginCommandHandlerTests.cs`:

```csharp
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
        _dbContext.Users.Returns(users.BuildMockDbSet());
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~LoginCommandHandlerTests"`
Expected: FAIL (build error — `LoginCommand` does not exist).

- [ ] **Step 3: Write the response DTO**

Create `src/Application/Users/Data/LoginResponse.cs`:

```csharp
namespace Application.Users.Data;

public sealed record LoginResponse(
    string Token,
    Guid UserId,
    string Email,
    string Username,
    string DisplayName);
```

- [ ] **Step 4: Write the command**

Create `src/Application/Users/LoginCommand.cs`:

```csharp
using Application.Abstractions.Messaging;
using Application.Users.Data;

namespace Application.Users;

public sealed record LoginCommand(string Identifier, string Password) : ICommand<LoginResponse>;
```

- [ ] **Step 5: Write the handler**

Create `src/Application/Users/LoginCommandHandler.cs`:

```csharp
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Users.Data;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users;

internal sealed class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider)
    : ICommandHandler<LoginCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        string identifier = (command.Identifier ?? string.Empty).Trim().ToLowerInvariant();

        User? user = await dbContext.Users
            .FirstOrDefaultAsync(
                u => u.Email == identifier || u.Username == identifier,
                cancellationToken);

        if (user is null || !passwordHasher.Verify(command.Password ?? string.Empty, user.PasswordHash))
        {
            return Result.Failure<LoginResponse>(UserErrors.InvalidCredentials);
        }

        string token = tokenProvider.Create(user);

        return new LoginResponse(token, user.Id, user.Email, user.Username, user.DisplayName);
    }
}
```

- [ ] **Step 6: Register the handler**

In `src/Application/DependencyInjection.cs`, add the login registration and the `LoginResponse` using. Replace file:

```csharp
using Application.Abstractions.Messaging;
using Application.Users;
using Application.Users.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<RegisterUserCommand, Guid>, RegisterUserCommandHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, LoginResponse>, LoginCommandHandler>();
        return services;
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~LoginCommandHandlerTests"`
Expected: PASS (4 tests green).

- [ ] **Step 8: Commit**

```bash
git add src/Application/Users src/Application/DependencyInjection.cs tests/Application.UnitTests/Users/LoginCommandHandlerTests.cs
git commit -m "feat: add Login command, handler returning JWT, and unit tests

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A8: `AlphabetLetter` entity + `GetAlphabetQuery`

**Files:**
- Create: `src/Domain/Alphabets/AlphabetLetter.cs`
- Create: `src/Application/Alphabets/Data/AlphabetLetterResponse.cs`
- Create: `src/Application/Alphabets/GetAlphabetQuery.cs`
- Create: `src/Application/Alphabets/GetAlphabetQueryHandler.cs`
- Modify: `src/Application/Abstractions/Data/IApplicationDbContext.cs`
- Modify: `src/Infrastructure/Database/ApplicationDbContext.cs`
- Modify: `src/Application/DependencyInjection.cs`
- Test: `tests/Application.UnitTests/Alphabets/GetAlphabetQueryHandlerTests.cs`

**Interfaces:**
- Produces: `AlphabetLetter : Entity` with public props `UpperCase, LowerCase, Name, Sound, ExampleWord, ExampleEmoji, DisplayOrder` (all get; init via object initializer inside Domain); `AlphabetLetterResponse(Guid Id, string UpperCase, string LowerCase, string Name, string Sound, string ExampleWord, string ExampleEmoji, int DisplayOrder)`; `GetAlphabetQuery() : IQuery<IReadOnlyList<AlphabetLetterResponse>>`; `IApplicationDbContext.AlphabetLetters : DbSet<AlphabetLetter>`.

- [ ] **Step 1: Write the failing test**

Create `tests/Application.UnitTests/Alphabets/GetAlphabetQueryHandlerTests.cs`:

```csharp
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
        _dbContext.AlphabetLetters.Returns(letters.BuildMockDbSet());
        var handler = new GetAlphabetQueryHandler(_dbContext);

        var result = await handler.Handle(new GetAlphabetQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].UpperCase.ShouldBe("A");
        result.Value[1].UpperCase.ShouldBe("B");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~GetAlphabetQueryHandlerTests"`
Expected: FAIL (build error — `Domain.Alphabets.AlphabetLetter` does not exist).

- [ ] **Step 3: Write the entity**

Create `src/Domain/Alphabets/AlphabetLetter.cs`:

```csharp
using SharedKernel;

namespace Domain.Alphabets;

public sealed class AlphabetLetter : Entity
{
    public string UpperCase { get; init; } = string.Empty;

    public string LowerCase { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Sound { get; init; } = string.Empty;

    public string ExampleWord { get; init; } = string.Empty;

    public string ExampleEmoji { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }
}
```

Note: the `UpperCase`/`LowerCase`/… members are `public init`, so a test in `Application.UnitTests` can set them in an object initializer. `Entity.Id` has a `protected internal` setter, so the test does **not** set `Id` (it defaults to `Guid.Empty`, which is fine — the ordering assertions only look at `DisplayOrder`/`UpperCase`). Infrastructure seeding (Task A9) sets `Id` via anonymous objects, which EF applies through model metadata rather than the setter.

- [ ] **Step 4: Write the response DTO**

Create `src/Application/Alphabets/Data/AlphabetLetterResponse.cs`:

```csharp
namespace Application.Alphabets.Data;

public sealed record AlphabetLetterResponse(
    Guid Id,
    string UpperCase,
    string LowerCase,
    string Name,
    string Sound,
    string ExampleWord,
    string ExampleEmoji,
    int DisplayOrder);
```

- [ ] **Step 5: Write the query**

Create `src/Application/Alphabets/GetAlphabetQuery.cs`:

```csharp
using Application.Abstractions.Messaging;
using Application.Alphabets.Data;

namespace Application.Alphabets;

public sealed record GetAlphabetQuery : IQuery<IReadOnlyList<AlphabetLetterResponse>>;
```

- [ ] **Step 6: Write the handler**

Create `src/Application/Alphabets/GetAlphabetQueryHandler.cs`:

```csharp
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Alphabets.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Alphabets;

internal sealed class GetAlphabetQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAlphabetQuery, IReadOnlyList<AlphabetLetterResponse>>
{
    public async Task<Result<IReadOnlyList<AlphabetLetterResponse>>> Handle(
        GetAlphabetQuery query,
        CancellationToken cancellationToken)
    {
        List<AlphabetLetterResponse> letters = await dbContext.AlphabetLetters
            .OrderBy(l => l.DisplayOrder)
            .Select(l => new AlphabetLetterResponse(
                l.Id,
                l.UpperCase,
                l.LowerCase,
                l.Name,
                l.Sound,
                l.ExampleWord,
                l.ExampleEmoji,
                l.DisplayOrder))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AlphabetLetterResponse>>(letters);
    }
}
```

- [ ] **Step 7: Expose `AlphabetLetters` on the context**

In `src/Application/Abstractions/Data/IApplicationDbContext.cs`, add the DbSet (and `using Domain.Alphabets;`):

```csharp
using Domain.Alphabets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<AlphabetLetter> AlphabetLetters { get; }

    DbSet<T> Set<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

In `src/Infrastructure/Database/ApplicationDbContext.cs`, add the DbSet (and `using Domain.Alphabets;`):

```csharp
using Application.Abstractions.Data;
using Domain.Alphabets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<AlphabetLetter> AlphabetLetters => Set<AlphabetLetter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

- [ ] **Step 8: Register the handler**

In `src/Application/DependencyInjection.cs`, add alphabet query registration. Replace file:

```csharp
using Application.Abstractions.Messaging;
using Application.Alphabets;
using Application.Alphabets.Data;
using Application.Users;
using Application.Users.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<RegisterUserCommand, Guid>, RegisterUserCommandHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, LoginResponse>, LoginCommandHandler>();
        services.AddScoped<IQueryHandler<GetAlphabetQuery, IReadOnlyList<AlphabetLetterResponse>>, GetAlphabetQueryHandler>();
        return services;
    }
}
```

- [ ] **Step 9: Run tests to verify they pass**

Run: `dotnet test tests/Application.UnitTests/`
Expected: PASS (all unit tests: User, Register, Login, Alphabet). If the `Id = Guid.NewGuid()` initializer in the test fails to compile, remove those two `Id = ...,` fragments and re-run.

- [ ] **Step 10: Commit**

```bash
git add src/Domain/Alphabets src/Application/Alphabets src/Application/Abstractions/Data/IApplicationDbContext.cs src/Infrastructure/Database/ApplicationDbContext.cs src/Application/DependencyInjection.cs tests/Application.UnitTests/Alphabets
git commit -m "feat: add AlphabetLetter entity and GetAlphabet query with unit test

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A9: Infrastructure — PBKDF2 hasher, JWT provider, EF configs + seed data

**Files:**
- Create: `src/Infrastructure/Authentication/Pbkdf2PasswordHasher.cs`
- Create: `src/Infrastructure/Authentication/JwtTokenProvider.cs`
- Create: `src/Infrastructure/Users/UserConfiguration.cs`
- Create: `src/Infrastructure/Alphabets/AlphabetLetterConfiguration.cs`
- Test: `tests/Application.UnitTests/` — none (hasher is exercised by integration tests; keep unit tests port-based)

**Interfaces:**
- Produces: `Pbkdf2PasswordHasher : IPasswordHasher`, `JwtTokenProvider : ITokenProvider` (both `internal sealed`); EF configs mapping `users` and `alphabet_letters` tables + `HasData` seed (29 letters + demo user).

- [ ] **Step 1: Write the PBKDF2 password hasher**

Create `src/Infrastructure/Authentication/Pbkdf2PasswordHasher.cs`:

```csharp
using System.Globalization;
using System.Security.Cryptography;
using Application.Abstractions.Authentication;

namespace Infrastructure.Authentication;

/// <summary>
/// PBKDF2-HMAC-SHA256 password hasher built on the BCL (no third-party crypto package,
/// and no Microsoft.AspNetCore.* namespace that would trip the architecture tests).
/// Stored format: pbkdf2-sha256$&lt;iterations&gt;$&lt;base64 salt&gt;$&lt;base64 key&gt;
/// </summary>
internal sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 600_000;
    private const char Delimiter = '$';
    private const string Prefix = "pbkdf2-sha256";
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return string.Join(
            Delimiter,
            Prefix,
            Iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(key));
    }

    public bool Verify(string password, string passwordHash)
    {
        string[] parts = passwordHash.Split(Delimiter);
        if (parts.Length != 4 || parts[0] != Prefix)
        {
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int iterations))
        {
            return false;
        }

        byte[] salt = Convert.FromBase64String(parts[2]);
        byte[] key = Convert.FromBase64String(parts[3]);
        byte[] attempted = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, key.Length);

        return CryptographicOperations.FixedTimeEquals(attempted, key);
    }
}
```

- [ ] **Step 2: Write the JWT token provider**

Create `src/Infrastructure/Authentication/JwtTokenProvider.cs`:

```csharp
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Abstractions.Authentication;
using Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SharedKernel;

namespace Infrastructure.Authentication;

internal sealed class JwtTokenProvider(IConfiguration configuration, IDateTimeProvider clock)
    : ITokenProvider
{
    public string Create(User user)
    {
        string secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is missing.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("username", user.Username),
            new(JwtRegisteredClaimNames.Name, user.DisplayName)
        ];

        int minutes = int.Parse(
            configuration["Jwt:ExpirationInMinutes"] ?? "60",
            CultureInfo.InvariantCulture);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: clock.UtcNow.AddMinutes(minutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

- [ ] **Step 3: Write the `User` EF configuration**

Create `src/Infrastructure/Users/UserConfiguration.cs`:

```csharp
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.Username).HasMaxLength(30).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique();

        builder.Ignore(u => u.DomainEvents);

        builder.HasData(new
        {
            Id = new Guid("d1ea0000-0000-4000-8000-000000000100"),
            Email = "demo@dlearning.vn",
            Username = "demo",
            DisplayName = "Bé Demo",
            PasswordHash = "pbkdf2-sha256$600000$zmz5Jrdt8HVqMz3413OHZQ==$6f+Gd25WENriwukIIkMp+VFRVxMvbzPNjZ+11YdJXPw="
        });
    }
}
```

- [ ] **Step 4: Write the `AlphabetLetter` EF configuration + 29-letter seed**

Create `src/Infrastructure/Alphabets/AlphabetLetterConfiguration.cs`:

```csharp
using System.Globalization;
using Domain.Alphabets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Alphabets;

internal sealed class AlphabetLetterConfiguration : IEntityTypeConfiguration<AlphabetLetter>
{
    public void Configure(EntityTypeBuilder<AlphabetLetter> builder)
    {
        builder.ToTable("alphabet_letters");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.UpperCase).HasMaxLength(4).IsRequired();
        builder.Property(l => l.LowerCase).HasMaxLength(4).IsRequired();
        builder.Property(l => l.Name).HasMaxLength(32).IsRequired();
        builder.Property(l => l.Sound).HasMaxLength(32).IsRequired();
        builder.Property(l => l.ExampleWord).HasMaxLength(64).IsRequired();
        builder.Property(l => l.ExampleEmoji).HasMaxLength(16).IsRequired();
        builder.Property(l => l.DisplayOrder).IsRequired();

        builder.HasIndex(l => l.DisplayOrder).IsUnique();

        builder.Ignore(l => l.DomainEvents);

        builder.HasData(Seed());
    }

    private static object[] Seed()
    {
        (string Upper, string Lower, string Name, string Sound, string Word, string Emoji)[] rows =
        [
            ("A", "a", "a", "a", "áo", "👕"),
            ("Ă", "ă", "á", "á", "ăn", "🍚"),
            ("Â", "â", "ớ", "ớ", "ấm", "🫖"),
            ("B", "b", "bê", "bờ", "bò", "🐄"),
            ("C", "c", "xê", "cờ", "cá", "🐟"),
            ("D", "d", "dê", "dờ", "dép", "🩴"),
            ("Đ", "đ", "đê", "đờ", "đèn", "💡"),
            ("E", "e", "e", "e", "em bé", "👶"),
            ("Ê", "ê", "ê", "ê", "ếch", "🐸"),
            ("G", "g", "giê", "gờ", "gà", "🐔"),
            ("H", "h", "hát", "hờ", "hoa", "🌸"),
            ("I", "i", "i", "i", "im lặng", "🤫"),
            ("K", "k", "ca", "cờ", "kẹo", "🍬"),
            ("L", "l", "e-lờ", "lờ", "lá", "🍃"),
            ("M", "m", "em-mờ", "mờ", "mèo", "🐱"),
            ("N", "n", "en-nờ", "nờ", "nón", "👒"),
            ("O", "o", "o", "o", "ong", "🐝"),
            ("Ô", "ô", "ô", "ô", "ô tô", "🚗"),
            ("Ơ", "ơ", "ơ", "ơ", "quả mơ", "🍑"),
            ("P", "p", "pê", "pờ", "pin", "🔋"),
            ("Q", "q", "quy", "quờ", "quà", "🎁"),
            ("R", "r", "e-rờ", "rờ", "rùa", "🐢"),
            ("S", "s", "ét-sì", "sờ", "sao", "⭐"),
            ("T", "t", "tê", "tờ", "táo", "🍎"),
            ("U", "u", "u", "u", "uống nước", "🥤"),
            ("Ư", "ư", "ư", "ư", "sư tử", "🦁"),
            ("V", "v", "vê", "vờ", "voi", "🐘"),
            ("X", "x", "ích-xì", "xờ", "xe đạp", "🚲"),
            ("Y", "y", "i dài", "i", "yêu", "❤️")
        ];

        var seed = new object[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            int order = i + 1;
            string hex = order.ToString("x2", CultureInfo.InvariantCulture);
            seed[i] = new
            {
                Id = new Guid("d1ea0000-0000-4000-8000-0000000000" + hex),
                UpperCase = rows[i].Upper,
                LowerCase = rows[i].Lower,
                Name = rows[i].Name,
                Sound = rows[i].Sound,
                ExampleWord = rows[i].Word,
                ExampleEmoji = rows[i].Emoji,
                DisplayOrder = order
            };
        }

        return seed;
    }
}
```

The anonymous-object property names (`UpperCase`, `LowerCase`, …) must match the entity's property names exactly, or EF's `HasData` throws at model-build time.

- [ ] **Step 5: Build**

Run: `dotnet build DLearning.slnx`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add src/Infrastructure/Authentication src/Infrastructure/Users src/Infrastructure/Alphabets
git commit -m "feat: add PBKDF2 hasher, JWT provider, EF configs and 29-letter seed

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A10: Wire DI, JWT authentication, and the dev migration runner

**Files:**
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Create: `src/Infrastructure/Database/MigrationExtensions.cs`
- Modify: `src/Web.Api/Program.cs`
- Modify: `src/Web.Api/appsettings.json`

**Interfaces:**
- Produces: `IServiceProvider.ApplyMigrations()` extension (Infrastructure, public — keeps `ApplicationDbContext` internal); DI registrations for `IPasswordHasher`, `ITokenProvider`; JWT bearer auth in Web.Api.

- [ ] **Step 1: Register hasher + token provider in Infrastructure DI**

Replace `src/Infrastructure/DependencyInjection.cs`:

```csharp
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Infrastructure.Authentication;
using Infrastructure.Database;
using Infrastructure.Database.Interceptors;
using Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<AuditingInterceptor>();

        string connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is missing.");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ITokenProvider, JwtTokenProvider>();

        return services;
    }
}
```

- [ ] **Step 2: Write the migration runner extension**

Create `src/Infrastructure/Database/MigrationExtensions.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Database;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        ApplicationDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
    }
}
```

- [ ] **Step 3: Add JWT config to `appsettings.json`**

In `src/Web.Api/appsettings.json`, add a `Jwt` section (top level, sibling of `ConnectionStrings`):

```json
  "Jwt": {
    "Secret": "dlearning-super-secret-signing-key-change-me-1234567890",
    "Issuer": "dlearning",
    "Audience": "dlearning-web",
    "ExpirationInMinutes": 60
  },
```

The full file becomes:

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5432;Database=dlearning;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "dlearning-super-secret-signing-key-change-me-1234567890",
    "Issuer": "dlearning",
    "Audience": "dlearning-web",
    "ExpirationInMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 4: Wire authentication + CORS + dev migration into `Program.cs`**

Replace `src/Web.Api/Program.cs`:

```csharp
using System.Text;
using Application;
using Infrastructure;
using Infrastructure.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Web.Api.Infrastructure;
using Web.Api.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpoints(typeof(Program).Assembly);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });
builder.Services.AddAuthorization();

const string CorsPolicy = "dlearning-web";
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()));

WebApplication app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.Services.ApplyMigrations();
}

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.Run();

public partial class Program;
```

Note: CORS is a belt-and-suspenders convenience; the frontend primarily uses the Vite dev proxy (Task B). Both work.

- [ ] **Step 5: Build**

Run: `dotnet build DLearning.slnx`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 6: Run the architecture tests (Infrastructure must still be web-framework-free)**

Run: `dotnet test tests/ArchitectureTests/`
Expected: PASS. In particular `Infrastructure_Should_NotDependOn_WebFrameworks` stays green — the JWT *token creation* lives in `System.IdentityModel.Tokens.Jwt` / `Microsoft.IdentityModel.Tokens` (not `Microsoft.AspNetCore`), and the JWT *bearer middleware* lives only in Web.Api.

- [ ] **Step 7: Commit**

```bash
git add src/Infrastructure/DependencyInjection.cs src/Infrastructure/Database/MigrationExtensions.cs src/Web.Api/Program.cs src/Web.Api/appsettings.json
git commit -m "feat: wire JWT auth, DI registrations, CORS, and dev auto-migration

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A11: HTTP endpoints (register, login, alphabet)

**Files:**
- Create: `src/Web.Api/Endpoints/Users/RegisterUser.cs`
- Create: `src/Web.Api/Endpoints/Users/Login.cs`
- Create: `src/Web.Api/Endpoints/Alphabets/GetAlphabet.cs`

**Interfaces:**
- Consumes: handlers via `ICommandHandler<,>` / `IQueryHandler<,>`, `ResultExtensions.ToHttpResult`.
- Produces: `POST /users/register`, `POST /users/login`, `GET /alphabet` (auth-required).

- [ ] **Step 1: Write the register endpoint**

Create `src/Web.Api/Endpoints/Users/RegisterUser.cs`:

```csharp
using Application.Abstractions.Messaging;
using Application.Users;
using SharedKernel;
using Web.Api.Infrastructure;
using Web.Api.Middleware;

namespace Web.Api.Endpoints.Users;

internal sealed class RegisterUser : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/users/register", async (
            RegisterUserCommand command,
            ICommandHandler<RegisterUserCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.ToHttpResult(id => Results.Ok(new { id }));
        });
    }
}
```

- [ ] **Step 2: Write the login endpoint**

Create `src/Web.Api/Endpoints/Users/Login.cs`:

```csharp
using Application.Abstractions.Messaging;
using Application.Users;
using Application.Users.Data;
using SharedKernel;
using Web.Api.Infrastructure;
using Web.Api.Middleware;

namespace Web.Api.Endpoints.Users;

internal sealed class Login : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/users/login", async (
            LoginCommand command,
            ICommandHandler<LoginCommand, LoginResponse> handler,
            CancellationToken cancellationToken) =>
        {
            Result<LoginResponse> result = await handler.Handle(command, cancellationToken);

            return result.ToHttpResult();
        });
    }
}
```

- [ ] **Step 3: Write the alphabet endpoint (auth-required)**

Create `src/Web.Api/Endpoints/Alphabets/GetAlphabet.cs`:

```csharp
using Application.Abstractions.Messaging;
using Application.Alphabets;
using Application.Alphabets.Data;
using SharedKernel;
using Web.Api.Infrastructure;
using Web.Api.Middleware;

namespace Web.Api.Endpoints.Alphabets;

internal sealed class GetAlphabet : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/alphabet", async (
            IQueryHandler<GetAlphabetQuery, IReadOnlyList<AlphabetLetterResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<AlphabetLetterResponse>> result =
                await handler.Handle(new GetAlphabetQuery(), cancellationToken);

            return result.ToHttpResult();
        })
        .RequireAuthorization();
    }
}
```

- [ ] **Step 4: Build**

Run: `dotnet build DLearning.slnx`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add src/Web.Api/Endpoints
git commit -m "feat: add register, login, and alphabet HTTP endpoints

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A12: EF migration + run the API

**Files:**
- Create: `src/Infrastructure/Database/Migrations/*_Initial.cs` (generated)

- [ ] **Step 1: Ensure the database is running**

Run: `cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be && docker compose up -d && docker compose ps`
Expected: `dlearning-postgres` healthy.

- [ ] **Step 2: Restore the EF tool and create the migration**

`dotnet ef` builds the app host to discover the DbContext, which runs the top-level code up to `app.Run()` — including the Development-only `ApplyMigrations()`. Set `ASPNETCORE_ENVIRONMENT=Production` for the EF commands so that block is skipped and the tool controls migrations itself:

```bash
dotnet tool restore
ASPNETCORE_ENVIRONMENT=Production dotnet ef migrations add Initial --project src/Infrastructure --startup-project src/Web.Api
```
Expected: `Done.` and new files under `src/Infrastructure/Database/Migrations/` whose `Up()` creates `users` + `alphabet_letters` tables, the two unique user indexes, the unique `DisplayOrder` index, and `InsertData` for the demo user + 29 letters.

- [ ] **Step 3: Apply the migration**

Run: `ASPNETCORE_ENVIRONMENT=Production dotnet ef database update --project src/Infrastructure --startup-project src/Web.Api`
Expected: `Applying migration '..._Initial'. Done.`

- [ ] **Step 4: Verify the seed landed**

Run: `docker exec dlearning-postgres psql -U postgres -d dlearning -c "select count(*) from alphabet_letters;"`
Expected: `29`

Run: `docker exec dlearning-postgres psql -U postgres -d dlearning -c "select email, username from users;"`
Expected: one row — `demo@dlearning.vn | demo`

- [ ] **Step 5: Smoke-test the API manually**

Run (in one shell): `dotnet run --project src/Web.Api`
Then in another shell:
```bash
# login with the seeded demo account
curl -s -X POST http://localhost:5113/users/login \
  -H 'Content-Type: application/json' \
  -d '{"identifier":"demo@dlearning.vn","password":"Demo@123"}'
```
Expected: JSON containing a non-empty `token`, plus `username":"demo"`.

```bash
# alphabet without token → 401
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5113/alphabet
```
Expected: `401`

```bash
# alphabet with token → 200 + 29 items
TOKEN=$(curl -s -X POST http://localhost:5113/users/login -H 'Content-Type: application/json' -d '{"identifier":"demo","password":"Demo@123"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
curl -s http://localhost:5113/alphabet -H "Authorization: Bearer $TOKEN" | grep -o '"upperCase"' | wc -l
```
Expected: `29`

Stop the API (Ctrl-C) when done.

- [ ] **Step 6: Commit**

```bash
git add src/Infrastructure/Database/Migrations
git commit -m "feat: add Initial EF migration with users + alphabet schema and seed

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A13: Integration tests

**Files:**
- Modify: `tests/Api.IntegrationTests/Infrastructure/ApiTestFactory.cs` (add Jwt config to the in-memory settings)
- Create: `tests/Api.IntegrationTests/Users/AuthEndpointsTests.cs`
- Create: `tests/Api.IntegrationTests/Alphabets/AlphabetEndpointsTests.cs`

**Interfaces:**
- Consumes: `ApiTestFactory` (Testcontainers Postgres + `WebApplicationFactory<Program>`).

- [ ] **Step 1: Give the test host JWT config**

Replace the `ConfigureWebHost` body's in-memory collection in `tests/Api.IntegrationTests/Infrastructure/ApiTestFactory.cs` so it also supplies Jwt settings:

```csharp
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _dbContainer.GetConnectionString(),
                ["Jwt:Secret"] = "test-signing-key-test-signing-key-1234567890-abcdef",
                ["Jwt:Issuer"] = "dlearning",
                ["Jwt:Audience"] = "dlearning-web",
                ["Jwt:ExpirationInMinutes"] = "60",
            });
        });
    }
```

(Everything else in `ApiTestFactory` is unchanged — it still creates the container in a field and calls `MigrateAsync` in `InitializeAsync`. Because the migration seeds data, the demo user and 29 letters exist in every test container.)

- [ ] **Step 2: Write the auth endpoint tests**

Create `tests/Api.IntegrationTests/Users/AuthEndpointsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Api.IntegrationTests.Users;

public sealed class AuthEndpointsTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_WithSeededDemoAccount_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/users/login",
            new { identifier = "demo@dlearning.vn", password = "Demo@123" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginBody>();
        body.ShouldNotBeNull();
        body.Token.ShouldNotBeNullOrWhiteSpace();
        body.Username.ShouldBe("demo");
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/users/login",
            new { identifier = "demo", password = "nope" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_ThenLogin_Succeeds()
    {
        var register = await _client.PostAsJsonAsync("/users/register",
            new { email = "na@dlearning.vn", username = "bena", displayName = "Bé Na", password = "Secret123" });
        register.StatusCode.ShouldBe(HttpStatusCode.OK);

        var login = await _client.PostAsJsonAsync("/users/login",
            new { identifier = "bena", password = "Secret123" });
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var first = await _client.PostAsJsonAsync("/users/register",
            new { email = "dup@dlearning.vn", username = "dupuser1", displayName = "Dup", password = "Secret123" });
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        var second = await _client.PostAsJsonAsync("/users/register",
            new { email = "dup@dlearning.vn", username = "dupuser2", displayName = "Dup", password = "Secret123" });
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/users/register",
            new { email = "short@dlearning.vn", username = "shorty", displayName = "Short", password = "123" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private sealed record LoginBody(string Token, Guid UserId, string Email, string Username, string DisplayName);
}
```

- [ ] **Step 3: Write the alphabet endpoint tests**

Create `tests/Api.IntegrationTests/Alphabets/AlphabetEndpointsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Api.IntegrationTests.Alphabets;

public sealed class AlphabetEndpointsTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAlphabet_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/alphabet");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAlphabet_WithToken_Returns29LettersInOrder()
    {
        var login = await _client.PostAsJsonAsync("/users/login",
            new { identifier = "demo", password = "Demo@123" });
        var auth = await login.Content.ReadFromJsonAsync<TokenOnly>();
        auth.ShouldNotBeNull();

        var request = new HttpRequestMessage(HttpMethod.Get, "/alphabet");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var letters = await response.Content.ReadFromJsonAsync<List<LetterBody>>();
        letters.ShouldNotBeNull();
        letters.Count.ShouldBe(29);
        letters[0].UpperCase.ShouldBe("A");
        letters[3].UpperCase.ShouldBe("B");
        letters[^1].UpperCase.ShouldBe("Y");
    }

    private sealed record TokenOnly(string Token);
    private sealed record LetterBody(Guid Id, string UpperCase, string LowerCase, string Name, string Sound, string ExampleWord, string ExampleEmoji, int DisplayOrder);
}
```

- [ ] **Step 4: Run the full backend test suite (needs Docker)**

Run: `cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be && dotnet test`
Expected: PASS — ArchitectureTests + Application.UnitTests + Api.IntegrationTests all green.

- [ ] **Step 5: Commit**

```bash
git add tests/Api.IntegrationTests
git commit -m "test: add integration tests for auth and alphabet endpoints

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

# Phase B — Frontend (`dlearning-web`)

> All paths below are relative to `/Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-web`. Commit these in the `dlearning-web` git repo.

## Task B1: Scaffold Vite + React + TS, wire proxy, fonts, tokens

**Files:**
- Create (generated): Vite React-TS project files
- Create: `vite.config.ts`, `src/styles/global.css`, `src/styles/tokens.ts`, `src/main.tsx`
- Delete: `src/App.tsx`, `src/App.css`, `src/index.css`, `src/assets/` (create-vite defaults)

- [ ] **Step 1: Scaffold into the (non-empty) repo folder**

Run:
```bash
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-web
npm create vite@latest _scaffold -- --template react-ts
rsync -a _scaffold/ ./
rm -rf _scaffold
npm install
npm install react-router-dom
npm install -D vitest jsdom @testing-library/react @testing-library/jest-dom @testing-library/user-event
```
Expected: `node_modules/` populated; `package.json` lists `react`, `react-dom`, `react-router-dom` and dev-deps `vite`, `vitest`, `jsdom`, testing-library.

- [ ] **Step 2: Add the `test` script**

In `package.json`, add to `"scripts"`: `"test": "vitest run"` (keep the generated `dev`, `build`, `preview`).

- [ ] **Step 3: Replace `vite.config.ts` with proxy + vitest config**

Replace `vite.config.ts`:

```ts
/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5113',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
  },
});
```

- [ ] **Step 4: Remove default sample files**

```bash
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-web
rm -f src/App.tsx src/App.css src/index.css
rm -rf src/assets
```

- [ ] **Step 5: Add global styles + design tokens**

Create `src/styles/global.css`:

```css
@import url('https://fonts.googleapis.com/css2?family=Sora:wght@500;600;700;800&family=Plus+Jakarta+Sans:wght@400;500;600;700&display=swap');

* { box-sizing: border-box; }
body {
  margin: 0;
  background: #E9E7E0;
  font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
  -webkit-font-smoothing: antialiased;
  color: #1B1B2F;
}
input { font-family: inherit; }
::placeholder { color: #A6A6B5; }

@keyframes floaty { 0%, 100% { transform: translateY(0); } 50% { transform: translateY(-14px); } }
@keyframes fadeUp { from { opacity: 0; transform: translateY(14px); } to { opacity: 1; transform: translateY(0); } }

.auth-shell { display: flex; min-height: 100vh; }
.auth-form { flex: 1; display: flex; align-items: center; justify-content: center; padding: 40px; background: #FDFDFB; }
@media (max-width: 860px) { .auth-brand { display: none !important; } }
```

Create `src/styles/tokens.ts`:

```ts
export const brandGradient = 'linear-gradient(158deg,#4F46E5 0%,#6D28D9 55%,#7C3AED 100%)';

// Rotated across the 29 letter cards for colourful variety.
export const letterCovers = [
  'linear-gradient(135deg,#FF9A6B,#FF6B4A)',
  'linear-gradient(135deg,#38BDF8,#0EA5E9)',
  'linear-gradient(135deg,#34D399,#10B981)',
  'linear-gradient(135deg,#6D28D9,#4F46E5)',
  'linear-gradient(135deg,#F472B6,#EC4899)',
  'linear-gradient(135deg,#FBBF24,#F59E0B)',
];
```

- [ ] **Step 6: Replace `src/main.tsx` (router + auth provider wiring comes in B5; temporary minimal render for now)**

Replace `src/main.tsx`:

```tsx
import React from 'react';
import ReactDOM from 'react-dom/client';
import './styles/global.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <div style={{ padding: 40, fontFamily: 'Sora' }}>dLearning — scaffold OK</div>
  </React.StrictMode>,
);
```

- [ ] **Step 7: Verify build + dev server start**

Run: `npm run build`
Expected: `tsc -b` + `vite build` succeed, `dist/` produced, no type errors.

- [ ] **Step 8: Commit**

```bash
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-web
git add -A
git commit -m "chore: scaffold Vite React-TS app with proxy, fonts, and design tokens

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task B2: Domain layer — models + repository ports

**Files:**
- Create: `src/domain/models/User.ts`, `src/domain/models/AuthSession.ts`, `src/domain/models/AlphabetLetter.ts`
- Create: `src/domain/repositories/AuthRepository.ts`, `src/domain/repositories/AlphabetRepository.ts`

**Interfaces:**
- Produces: `User`, `AuthSession`, `AlphabetLetter` interfaces; `AuthRepository { login(LoginInput): Promise<AuthSession>; register(RegisterInput): Promise<void> }`; `AlphabetRepository { getAll(): Promise<AlphabetLetter[]> }`; input types `LoginInput`, `RegisterInput`.

- [ ] **Step 1: Write the models**

Create `src/domain/models/User.ts`:

```ts
export interface User {
  id: string;
  email: string;
  username: string;
  displayName: string;
}
```

Create `src/domain/models/AuthSession.ts`:

```ts
import type { User } from './User';

export interface AuthSession {
  token: string;
  user: User;
}
```

Create `src/domain/models/AlphabetLetter.ts`:

```ts
export interface AlphabetLetter {
  id: string;
  upperCase: string;
  lowerCase: string;
  name: string;
  sound: string;
  exampleWord: string;
  exampleEmoji: string;
  displayOrder: number;
}
```

- [ ] **Step 2: Write the repository ports**

Create `src/domain/repositories/AuthRepository.ts`:

```ts
import type { AuthSession } from '../models/AuthSession';

export interface LoginInput {
  identifier: string;
  password: string;
}

export interface RegisterInput {
  email: string;
  username: string;
  displayName: string;
  password: string;
}

export interface AuthRepository {
  login(input: LoginInput): Promise<AuthSession>;
  register(input: RegisterInput): Promise<void>;
}
```

Create `src/domain/repositories/AlphabetRepository.ts`:

```ts
import type { AlphabetLetter } from '../models/AlphabetLetter';

export interface AlphabetRepository {
  getAll(): Promise<AlphabetLetter[]>;
}
```

- [ ] **Step 3: Type-check**

Run: `npx tsc -b`
Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add src/domain
git commit -m "feat: add frontend domain models and repository ports

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task B3: Infrastructure layer — HTTP client, storage, API repositories

**Files:**
- Create: `src/infrastructure/http/ApiError.ts`, `src/infrastructure/http/HttpClient.ts`
- Create: `src/infrastructure/storage/TokenStorage.ts`
- Create: `src/infrastructure/repositories/ApiAuthRepository.ts`, `src/infrastructure/repositories/ApiAlphabetRepository.ts`

**Interfaces:**
- Consumes: domain ports + models.
- Produces: `ApiError(status, message, code?)`; `HttpClient({ baseUrl, getToken, onUnauthorized })` with `get<T>`/`post<T>`; `TokenStorage` with `save(session, remember)`, `load()`, `clear()`; `ApiAuthRepository`, `ApiAlphabetRepository` implementing the ports.

- [ ] **Step 1: Write `ApiError`**

Create `src/infrastructure/http/ApiError.ts`:

```ts
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly code?: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}
```

- [ ] **Step 2: Write `HttpClient`**

Create `src/infrastructure/http/HttpClient.ts`:

```ts
import { ApiError } from './ApiError';

export interface HttpClientOptions {
  baseUrl: string;
  getToken: () => string | null;
  onUnauthorized: () => void;
}

export class HttpClient {
  constructor(private readonly options: HttpClientOptions) {}

  post<T>(path: string, body: unknown): Promise<T> {
    return this.request<T>('POST', path, body);
  }

  get<T>(path: string): Promise<T> {
    return this.request<T>('GET', path);
  }

  private async request<T>(method: string, path: string, body?: unknown): Promise<T> {
    const token = this.options.getToken();
    const headers: Record<string, string> = { 'Content-Type': 'application/json' };
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    const response = await fetch(`${this.options.baseUrl}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    });

    if (response.status === 401) {
      this.options.onUnauthorized();
    }

    if (!response.ok) {
      throw await this.toError(response);
    }

    if (response.status === 204) {
      return undefined as T;
    }

    const text = await response.text();
    return (text ? JSON.parse(text) : undefined) as T;
  }

  private async toError(response: Response): Promise<ApiError> {
    let message = 'Đã có lỗi xảy ra. Vui lòng thử lại.';
    let code: string | undefined;
    try {
      const data = await response.json();
      message = data.description ?? data.detail ?? data.title ?? message;
      code = data.code ?? data.title;
    } catch {
      // Non-JSON error body — keep the friendly default message.
    }
    return new ApiError(response.status, message, code);
  }
}
```

- [ ] **Step 3: Write `TokenStorage`**

Create `src/infrastructure/storage/TokenStorage.ts`:

```ts
import type { AuthSession } from '../../domain/models/AuthSession';

const KEY = 'dlearning.session';

export class TokenStorage {
  save(session: AuthSession, remember: boolean): void {
    const target = remember ? localStorage : sessionStorage;
    const other = remember ? sessionStorage : localStorage;
    other.removeItem(KEY);
    target.setItem(KEY, JSON.stringify(session));
  }

  load(): AuthSession | null {
    const raw = localStorage.getItem(KEY) ?? sessionStorage.getItem(KEY);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as AuthSession;
    } catch {
      return null;
    }
  }

  clear(): void {
    localStorage.removeItem(KEY);
    sessionStorage.removeItem(KEY);
  }
}
```

- [ ] **Step 4: Write the API repositories**

Create `src/infrastructure/repositories/ApiAuthRepository.ts`:

```ts
import type { AuthSession } from '../../domain/models/AuthSession';
import type { AuthRepository, LoginInput, RegisterInput } from '../../domain/repositories/AuthRepository';
import type { HttpClient } from '../http/HttpClient';

interface LoginResponseDto {
  token: string;
  userId: string;
  email: string;
  username: string;
  displayName: string;
}

export class ApiAuthRepository implements AuthRepository {
  constructor(private readonly http: HttpClient) {}

  async login(input: LoginInput): Promise<AuthSession> {
    const dto = await this.http.post<LoginResponseDto>('/users/login', input);
    return {
      token: dto.token,
      user: {
        id: dto.userId,
        email: dto.email,
        username: dto.username,
        displayName: dto.displayName,
      },
    };
  }

  async register(input: RegisterInput): Promise<void> {
    await this.http.post<{ id: string }>('/users/register', input);
  }
}
```

Create `src/infrastructure/repositories/ApiAlphabetRepository.ts`:

```ts
import type { AlphabetLetter } from '../../domain/models/AlphabetLetter';
import type { AlphabetRepository } from '../../domain/repositories/AlphabetRepository';
import type { HttpClient } from '../http/HttpClient';

export class ApiAlphabetRepository implements AlphabetRepository {
  constructor(private readonly http: HttpClient) {}

  getAll(): Promise<AlphabetLetter[]> {
    return this.http.get<AlphabetLetter[]>('/alphabet');
  }
}
```

- [ ] **Step 5: Type-check**

Run: `npx tsc -b`
Expected: no errors.

- [ ] **Step 6: Commit**

```bash
git add src/infrastructure
git commit -m "feat: add HTTP client, token storage, and API repositories

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task B4: Application layer — use cases + unit tests

**Files:**
- Create: `src/application/usecases/login.ts`, `register.ts`, `getAlphabet.ts`
- Create: `src/application/usecases/register.test.ts`, `login.test.ts`, `getAlphabet.test.ts`
- Create: `src/infrastructure/storage/TokenStorage.test.ts`

**Interfaces:**
- Produces: `makeLogin(repo)`, `makeRegister(repo)`, `makeGetAlphabet(repo)` factory functions returning the callable use case.

- [ ] **Step 1: Write the failing tests**

Create `src/application/usecases/register.test.ts`:

```ts
import { describe, it, expect, vi } from 'vitest';
import { makeRegister } from './register';
import type { AuthRepository } from '../../domain/repositories/AuthRepository';

describe('register use case', () => {
  it('registers then auto-logs-in using the email', async () => {
    const session = { token: 'T', user: { id: '1', email: 'a@b.vn', username: 'u', displayName: 'U' } };
    const repo: AuthRepository = {
      register: vi.fn().mockResolvedValue(undefined),
      login: vi.fn().mockResolvedValue(session),
    };

    const register = makeRegister(repo);
    const result = await register({ email: 'a@b.vn', username: 'u', displayName: 'U', password: 'Secret123' });

    expect(repo.register).toHaveBeenCalledOnce();
    expect(repo.login).toHaveBeenCalledWith({ identifier: 'a@b.vn', password: 'Secret123' });
    expect(result).toBe(session);
  });
});
```

Create `src/application/usecases/login.test.ts`:

```ts
import { describe, it, expect, vi } from 'vitest';
import { makeLogin } from './login';
import type { AuthRepository } from '../../domain/repositories/AuthRepository';

describe('login use case', () => {
  it('delegates to the repository', async () => {
    const session = { token: 'T', user: { id: '1', email: 'a@b.vn', username: 'u', displayName: 'U' } };
    const repo: AuthRepository = {
      register: vi.fn(),
      login: vi.fn().mockResolvedValue(session),
    };

    const login = makeLogin(repo);
    const result = await login({ identifier: 'u', password: 'pw' });

    expect(repo.login).toHaveBeenCalledWith({ identifier: 'u', password: 'pw' });
    expect(result).toBe(session);
  });
});
```

Create `src/application/usecases/getAlphabet.test.ts`:

```ts
import { describe, it, expect, vi } from 'vitest';
import { makeGetAlphabet } from './getAlphabet';
import type { AlphabetRepository } from '../../domain/repositories/AlphabetRepository';

describe('getAlphabet use case', () => {
  it('returns the letters from the repository', async () => {
    const letters = [{ id: '1', upperCase: 'A', lowerCase: 'a', name: 'a', sound: 'a', exampleWord: 'áo', exampleEmoji: '👕', displayOrder: 1 }];
    const repo: AlphabetRepository = { getAll: vi.fn().mockResolvedValue(letters) };

    const getAlphabet = makeGetAlphabet(repo);
    const result = await getAlphabet();

    expect(result).toEqual(letters);
  });
});
```

Create `src/infrastructure/storage/TokenStorage.test.ts`:

```ts
import { describe, it, expect, beforeEach } from 'vitest';
import { TokenStorage } from './TokenStorage';

const session = { token: 'T', user: { id: '1', email: 'a@b.vn', username: 'u', displayName: 'U' } };

describe('TokenStorage', () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  it('persists to localStorage when remember=true', () => {
    const storage = new TokenStorage();
    storage.save(session, true);
    expect(localStorage.getItem('dlearning.session')).not.toBeNull();
    expect(sessionStorage.getItem('dlearning.session')).toBeNull();
    expect(storage.load()?.token).toBe('T');
  });

  it('persists to sessionStorage when remember=false', () => {
    const storage = new TokenStorage();
    storage.save(session, false);
    expect(sessionStorage.getItem('dlearning.session')).not.toBeNull();
    expect(localStorage.getItem('dlearning.session')).toBeNull();
  });

  it('clear removes the session from both stores', () => {
    const storage = new TokenStorage();
    storage.save(session, true);
    storage.clear();
    expect(storage.load()).toBeNull();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `npm test`
Expected: FAIL (cannot import `./login`, `./register`, `./getAlphabet`).

- [ ] **Step 3: Write the use cases**

Create `src/application/usecases/login.ts`:

```ts
import type { AuthSession } from '../../domain/models/AuthSession';
import type { AuthRepository, LoginInput } from '../../domain/repositories/AuthRepository';

export function makeLogin(repo: AuthRepository) {
  return (input: LoginInput): Promise<AuthSession> => repo.login(input);
}
```

Create `src/application/usecases/register.ts`:

```ts
import type { AuthSession } from '../../domain/models/AuthSession';
import type { AuthRepository, RegisterInput } from '../../domain/repositories/AuthRepository';

export function makeRegister(repo: AuthRepository) {
  return async (input: RegisterInput): Promise<AuthSession> => {
    await repo.register(input);
    return repo.login({ identifier: input.email, password: input.password });
  };
}
```

Create `src/application/usecases/getAlphabet.ts`:

```ts
import type { AlphabetLetter } from '../../domain/models/AlphabetLetter';
import type { AlphabetRepository } from '../../domain/repositories/AlphabetRepository';

export function makeGetAlphabet(repo: AlphabetRepository) {
  return (): Promise<AlphabetLetter[]> => repo.getAll();
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `npm test`
Expected: PASS (register, login, getAlphabet, TokenStorage — all green).

- [ ] **Step 5: Commit**

```bash
git add src/application src/infrastructure/storage/TokenStorage.test.ts
git commit -m "feat: add frontend use cases with unit tests

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task B5: DI container + auth context + router

**Files:**
- Create: `src/di/container.ts`
- Create: `src/presentation/auth/AuthContext.ts`, `AuthProvider.tsx`, `useAuth.ts`, `RequireAuth.tsx`
- Create: `src/presentation/router.tsx`
- Modify: `src/main.tsx`

**Interfaces:**
- Consumes: use cases, repositories, `HttpClient`, `TokenStorage`.
- Produces: `container` singleton `{ tokenStorage, login, register, getAlphabet }`; `useAuth()` → `{ session, setSession, logout }`; `RequireAuth`; `router`.

- [ ] **Step 1: Write the DI container (composition root)**

Create `src/di/container.ts`:

```ts
import { makeGetAlphabet } from '../application/usecases/getAlphabet';
import { makeLogin } from '../application/usecases/login';
import { makeRegister } from '../application/usecases/register';
import { HttpClient } from '../infrastructure/http/HttpClient';
import { ApiAlphabetRepository } from '../infrastructure/repositories/ApiAlphabetRepository';
import { ApiAuthRepository } from '../infrastructure/repositories/ApiAuthRepository';
import { TokenStorage } from '../infrastructure/storage/TokenStorage';

function createContainer() {
  const tokenStorage = new TokenStorage();

  const http = new HttpClient({
    baseUrl: '/api',
    getToken: () => tokenStorage.load()?.token ?? null,
    onUnauthorized: () => {
      tokenStorage.clear();
      if (window.location.pathname !== '/login') {
        window.location.assign('/login');
      }
    },
  });

  const authRepository = new ApiAuthRepository(http);
  const alphabetRepository = new ApiAlphabetRepository(http);

  return {
    tokenStorage,
    login: makeLogin(authRepository),
    register: makeRegister(authRepository),
    getAlphabet: makeGetAlphabet(alphabetRepository),
  };
}

export const container = createContainer();
```

- [ ] **Step 2: Write the auth context + provider + hook**

Create `src/presentation/auth/AuthContext.ts`:

```ts
import { createContext } from 'react';
import type { AuthSession } from '../../domain/models/AuthSession';

export interface AuthContextValue {
  session: AuthSession | null;
  setSession: (session: AuthSession, remember: boolean) => void;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
```

Create `src/presentation/auth/AuthProvider.tsx`:

```tsx
import { useMemo, useState, type ReactNode } from 'react';
import { container } from '../../di/container';
import type { AuthSession } from '../../domain/models/AuthSession';
import { AuthContext, type AuthContextValue } from './AuthContext';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSessionState] = useState<AuthSession | null>(() => container.tokenStorage.load());

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      setSession: (next, remember) => {
        container.tokenStorage.save(next, remember);
        setSessionState(next);
      },
      logout: () => {
        container.tokenStorage.clear();
        setSessionState(null);
      },
    }),
    [session],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
```

Create `src/presentation/auth/useAuth.ts`:

```ts
import { useContext } from 'react';
import { AuthContext } from './AuthContext';

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return ctx;
}
```

Create `src/presentation/auth/RequireAuth.tsx`:

```tsx
import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from './useAuth';

export function RequireAuth({ children }: { children: ReactNode }) {
  const { session } = useAuth();
  if (!session) {
    return <Navigate to="/login" replace />;
  }
  return <>{children}</>;
}
```

- [ ] **Step 3: Write the router**

Create `src/presentation/router.tsx`:

```tsx
import { createBrowserRouter, Navigate } from 'react-router-dom';
import { RequireAuth } from './auth/RequireAuth';
import { AlphabetPage } from './pages/AlphabetPage';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  { path: '/register', element: <RegisterPage /> },
  {
    path: '/alphabet',
    element: (
      <RequireAuth>
        <AlphabetPage />
      </RequireAuth>
    ),
  },
  { path: '*', element: <Navigate to="/alphabet" replace /> },
]);
```

- [ ] **Step 4: Rewire `main.tsx`**

Replace `src/main.tsx`:

```tsx
import React from 'react';
import ReactDOM from 'react-dom/client';
import { RouterProvider } from 'react-router-dom';
import { AuthProvider } from './presentation/auth/AuthProvider';
import { router } from './presentation/router';
import './styles/global.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <AuthProvider>
      <RouterProvider router={router} />
    </AuthProvider>
  </React.StrictMode>,
);
```

> Note: this references pages created in B6–B8; the type-check/build gate for this task is deferred to B8 Step (after the pages exist). Commit now; build is verified end-to-end in B8/B9.

- [ ] **Step 5: Commit**

```bash
git add src/di src/presentation/auth src/presentation/router.tsx src/main.tsx
git commit -m "feat: add DI container, auth context/provider, and router

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task B6: Shared UI components + LoginPage

**Files:**
- Create: `src/presentation/components/BrandPane.tsx`, `TextField.tsx`, `PrimaryButton.tsx`
- Create: `src/presentation/pages/LoginPage.tsx`

**Interfaces:**
- Produces: `BrandPane({ title, subtitle })`; `TextField({ label, type?, value, onChange, placeholder?, icon?, trailing? })`; `PrimaryButton({ children, type?, onClick?, disabled? })`; `LoginPage`.

- [ ] **Step 1: Write `BrandPane` (left gradient panel, reused by login + register)**

Create `src/presentation/components/BrandPane.tsx`:

```tsx
import { brandGradient } from '../../styles/tokens';

export function BrandPane({ title, subtitle }: { title: string; subtitle: string }) {
  return (
    <div
      className="auth-brand"
      style={{
        position: 'relative',
        overflow: 'hidden',
        color: '#fff',
        background: brandGradient,
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        padding: '48px 44px',
        width: '46%',
        minWidth: 320,
      }}
    >
      <div style={{ position: 'relative', display: 'flex', alignItems: 'center', gap: 11 }}>
        <div style={{ width: 40, height: 40, borderRadius: 12, background: 'rgba(255,255,255,.16)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none">
            <path d="M12 3L2 8l10 5 10-5-10-5z" fill="#fff" />
            <path d="M5 11v4.5c0 1 3 2.5 7 2.5s7-1.5 7-2.5V11" stroke="#fff" strokeWidth="1.6" fill="none" strokeLinecap="round" />
          </svg>
        </div>
        <span style={{ fontFamily: 'Sora', fontWeight: 800, fontSize: 20, letterSpacing: '-.02em' }}>dLearning</span>
      </div>

      <div style={{ position: 'relative' }}>
        <h1 style={{ fontFamily: 'Sora', fontWeight: 800, fontSize: 40, lineHeight: 1.08, letterSpacing: '-.03em', margin: '0 0 14px', whiteSpace: 'pre-line' }}>
          {title}
        </h1>
        <p style={{ fontSize: 15, lineHeight: 1.6, color: 'rgba(255,255,255,.82)', margin: 0, maxWidth: 340 }}>{subtitle}</p>
      </div>

      <div style={{ position: 'relative', display: 'flex', alignItems: 'center', gap: 20 }}>
        <div>
          <div style={{ fontFamily: 'Sora', fontWeight: 800, fontSize: 19 }}>20.000+</div>
          <div style={{ fontSize: 11.5, color: 'rgba(255,255,255,.7)' }}>học viên</div>
        </div>
        <div style={{ width: 1, height: 30, background: 'rgba(255,255,255,.2)' }} />
        <div>
          <div style={{ fontFamily: 'Sora', fontWeight: 800, fontSize: 19 }}>850+</div>
          <div style={{ fontSize: 11.5, color: 'rgba(255,255,255,.7)' }}>khóa học</div>
        </div>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Write `TextField`**

Create `src/presentation/components/TextField.tsx`:

```tsx
import type { ReactNode } from 'react';

interface TextFieldProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
  placeholder?: string;
  icon?: ReactNode;
  trailing?: ReactNode;
}

export function TextField({ label, value, onChange, type = 'text', placeholder, icon, trailing }: TextFieldProps) {
  return (
    <label style={{ display: 'block', marginBottom: 16 }}>
      <span style={{ display: 'block', fontSize: 12.5, fontWeight: 600, color: '#3A3A4D', marginBottom: 7 }}>{label}</span>
      <span style={{ position: 'relative', display: 'block' }}>
        {icon && <span style={{ position: 'absolute', left: 14, top: '50%', transform: 'translateY(-50%)', display: 'flex' }}>{icon}</span>}
        <input
          type={type}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          style={{
            width: '100%',
            height: 48,
            padding: icon ? '0 44px 0 40px' : '0 14px',
            borderRadius: 12,
            border: '1.5px solid #E4E2EC',
            background: '#fff',
            fontSize: 14,
            color: '#1B1B2F',
            outline: 'none',
          }}
        />
        {trailing && <span style={{ position: 'absolute', right: 6, top: '50%', transform: 'translateY(-50%)' }}>{trailing}</span>}
      </span>
    </label>
  );
}
```

- [ ] **Step 3: Write `PrimaryButton`**

Create `src/presentation/components/PrimaryButton.tsx`:

```tsx
import type { ReactNode } from 'react';

interface PrimaryButtonProps {
  children: ReactNode;
  type?: 'button' | 'submit';
  onClick?: () => void;
  disabled?: boolean;
}

export function PrimaryButton({ children, type = 'button', onClick, disabled }: PrimaryButtonProps) {
  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled}
      style={{
        width: '100%',
        height: 50,
        border: 'none',
        borderRadius: 13,
        background: 'linear-gradient(135deg,#4F46E5,#6D28D9)',
        color: '#fff',
        fontFamily: 'Sora',
        fontWeight: 700,
        fontSize: 15,
        cursor: disabled ? 'default' : 'pointer',
        boxShadow: '0 12px 26px -10px rgba(79,70,229,.7)',
        opacity: disabled ? 0.7 : 1,
      }}
    >
      {children}
    </button>
  );
}
```

- [ ] **Step 4: Write `LoginPage`**

Create `src/presentation/pages/LoginPage.tsx`:

```tsx
import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { container } from '../../di/container';
import { ApiError } from '../../infrastructure/http/ApiError';
import { BrandPane } from '../components/BrandPane';
import { PrimaryButton } from '../components/PrimaryButton';
import { TextField } from '../components/TextField';
import { useAuth } from '../auth/useAuth';

export function LoginPage() {
  const [identifier, setIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [showPass, setShowPass] = useState(false);
  const [remember, setRemember] = useState(true);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { setSession } = useAuth();
  const navigate = useNavigate();

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!identifier.trim() || !password) {
      setError('Vui lòng nhập đầy đủ thông tin đăng nhập.');
      return;
    }
    setLoading(true);
    setError('');
    try {
      const session = await container.login({ identifier, password });
      setSession(session, remember);
      navigate('/alphabet');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Không thể đăng nhập. Vui lòng thử lại.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-shell">
      <BrandPane
        title={'Học tập\nkhông giới hạn.'}
        subtitle="Nền tảng học trực tuyến đồng hành cùng mọi hành trình tri thức của bạn."
      />
      <div className="auth-form">
        <form onSubmit={submit} style={{ width: '100%', maxWidth: 380, animation: 'fadeUp .5s ease both' }}>
          <h2 style={{ fontFamily: 'Sora', fontWeight: 700, fontSize: 26, letterSpacing: '-.02em', margin: '0 0 6px' }}>Đăng nhập</h2>
          <p style={{ fontSize: 14, color: '#6B6B80', margin: '0 0 26px' }}>Chào mừng bạn quay lại dLearning.</p>

          {error && (
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, background: '#FEF2F2', border: '1px solid #FECACA', color: '#B91C1C', fontSize: 12.5, fontWeight: 500, padding: '10px 12px', borderRadius: 11, marginBottom: 16 }}>
              {error}
            </div>
          )}

          <TextField
            label="Email hoặc tên đăng nhập"
            value={identifier}
            onChange={setIdentifier}
            placeholder="you@example.com"
            icon={
              <svg width="17" height="17" viewBox="0 0 24 24" fill="none">
                <path d="M4 6h16v12H4z" stroke="#A6A6B5" strokeWidth="1.6" />
                <path d="M4 7l8 6 8-6" stroke="#A6A6B5" strokeWidth="1.6" />
              </svg>
            }
          />

          <TextField
            label="Mật khẩu"
            type={showPass ? 'text' : 'password'}
            value={password}
            onChange={setPassword}
            placeholder="••••••••"
            icon={
              <svg width="17" height="17" viewBox="0 0 24 24" fill="none">
                <rect x="5" y="10" width="14" height="10" rx="2" stroke="#A6A6B5" strokeWidth="1.6" />
                <path d="M8 10V7a4 4 0 018 0v3" stroke="#A6A6B5" strokeWidth="1.6" />
              </svg>
            }
            trailing={
              <button type="button" onClick={() => setShowPass((s) => !s)} aria-label="Hiện mật khẩu" style={{ width: 34, height: 34, border: 'none', background: 'none', cursor: 'pointer', color: '#A6A6B5', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                  <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7z" stroke="currentColor" strokeWidth="1.6" />
                  <circle cx="12" cy="12" r="2.6" stroke="currentColor" strokeWidth="1.6" />
                </svg>
              </button>
            }
          />

          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', margin: '6px 0 22px' }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer', fontSize: 13, color: '#5A5A6B' }}>
              <input type="checkbox" checked={remember} onChange={(e) => setRemember(e.target.checked)} />
              Ghi nhớ đăng nhập
            </label>
            <a href="#" style={{ fontSize: 13, fontWeight: 600, color: '#4F46E5', textDecoration: 'none' }}>Quên mật khẩu?</a>
          </div>

          <PrimaryButton type="submit" disabled={loading}>
            {loading ? 'Đang đăng nhập…' : 'Đăng nhập'}
          </PrimaryButton>

          <div style={{ display: 'flex', alignItems: 'center', gap: 12, margin: '22px 0' }}>
            <span style={{ flex: 1, height: 1, background: '#E4E2EC' }} />
            <span style={{ fontSize: 12, color: '#A6A6B5' }}>hoặc tiếp tục với</span>
            <span style={{ flex: 1, height: 1, background: '#E4E2EC' }} />
          </div>
          <div style={{ display: 'flex', gap: 12 }}>
            <button type="button" style={socialBtnStyle}>Google</button>
            <button type="button" style={socialBtnStyle}>Facebook</button>
          </div>

          <p style={{ textAlign: 'center', fontSize: 13, color: '#6B6B80', margin: '24px 0 0' }}>
            Chưa có tài khoản?{' '}
            <Link to="/register" style={{ fontWeight: 700, color: '#4F46E5', textDecoration: 'none' }}>Đăng ký ngay</Link>
          </p>
        </form>
      </div>
    </div>
  );
}

const socialBtnStyle: React.CSSProperties = {
  flex: 1,
  height: 46,
  border: '1.5px solid #E4E2EC',
  borderRadius: 12,
  background: '#fff',
  cursor: 'pointer',
  fontSize: 13.5,
  fontWeight: 600,
  color: '#3A3A4D',
};
```

Note: the Google/Facebook buttons are intentionally UI-only (no handler) per the spec's out-of-scope list.

- [ ] **Step 5: Commit**

```bash
git add src/presentation/components src/presentation/pages/LoginPage.tsx
git commit -m "feat: add shared UI components and LoginPage matching the design

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task B7: RegisterPage

**Files:**
- Create: `src/presentation/pages/RegisterPage.tsx`

**Interfaces:**
- Consumes: `BrandPane`, `TextField`, `PrimaryButton`, `container.register`, `useAuth`, `ApiError`.

- [ ] **Step 1: Write `RegisterPage`**

Create `src/presentation/pages/RegisterPage.tsx`:

```tsx
import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { container } from '../../di/container';
import { ApiError } from '../../infrastructure/http/ApiError';
import { BrandPane } from '../components/BrandPane';
import { PrimaryButton } from '../components/PrimaryButton';
import { TextField } from '../components/TextField';
import { useAuth } from '../auth/useAuth';

export function RegisterPage() {
  const [displayName, setDisplayName] = useState('');
  const [email, setEmail] = useState('');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { setSession } = useAuth();
  const navigate = useNavigate();

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!displayName.trim() || !email.trim() || !username.trim() || !password) {
      setError('Vui lòng điền đầy đủ thông tin.');
      return;
    }
    if (password.length < 8) {
      setError('Mật khẩu phải có ít nhất 8 ký tự.');
      return;
    }
    setLoading(true);
    setError('');
    try {
      const session = await container.register({ email, username, displayName, password });
      setSession(session, true);
      navigate('/alphabet');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Không thể đăng ký. Vui lòng thử lại.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-shell">
      <BrandPane
        title={'Bắt đầu\nhành trình học.'}
        subtitle="Tạo tài khoản dLearning để cùng bé khám phá bảng chữ cái tiếng Việt."
      />
      <div className="auth-form">
        <form onSubmit={submit} style={{ width: '100%', maxWidth: 380, animation: 'fadeUp .5s ease both' }}>
          <h2 style={{ fontFamily: 'Sora', fontWeight: 700, fontSize: 26, letterSpacing: '-.02em', margin: '0 0 6px' }}>Đăng ký</h2>
          <p style={{ fontSize: 14, color: '#6B6B80', margin: '0 0 26px' }}>Chỉ mất một phút để tạo tài khoản.</p>

          {error && (
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, background: '#FEF2F2', border: '1px solid #FECACA', color: '#B91C1C', fontSize: 12.5, fontWeight: 500, padding: '10px 12px', borderRadius: 11, marginBottom: 16 }}>
              {error}
            </div>
          )}

          <TextField label="Tên hiển thị" value={displayName} onChange={setDisplayName} placeholder="Bé Minh Anh" />
          <TextField label="Email" type="email" value={email} onChange={setEmail} placeholder="you@example.com" />
          <TextField label="Tên đăng nhập" value={username} onChange={setUsername} placeholder="minhanh (3–30 ký tự, chữ và số)" />
          <TextField label="Mật khẩu" type="password" value={password} onChange={setPassword} placeholder="Ít nhất 8 ký tự" />

          <div style={{ height: 6 }} />
          <PrimaryButton type="submit" disabled={loading}>
            {loading ? 'Đang tạo tài khoản…' : 'Đăng ký'}
          </PrimaryButton>

          <p style={{ textAlign: 'center', fontSize: 13, color: '#6B6B80', margin: '24px 0 0' }}>
            Đã có tài khoản?{' '}
            <Link to="/login" style={{ fontWeight: 700, color: '#4F46E5', textDecoration: 'none' }}>Đăng nhập</Link>
          </p>
        </form>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add src/presentation/pages/RegisterPage.tsx
git commit -m "feat: add RegisterPage with auto-login on success

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task B8: AlphabetPage + LetterCard + LetterDetail

**Files:**
- Create: `src/presentation/components/LetterCard.tsx`, `src/presentation/components/LetterDetail.tsx`
- Create: `src/presentation/pages/AlphabetPage.tsx`

**Interfaces:**
- Consumes: `container.getAlphabet`, `useAuth`, `AlphabetLetter`, `letterCovers`.

- [ ] **Step 1: Write `LetterCard`**

Create `src/presentation/components/LetterCard.tsx`:

```tsx
import type { AlphabetLetter } from '../../domain/models/AlphabetLetter';
import { letterCovers } from '../../styles/tokens';

interface LetterCardProps {
  letter: AlphabetLetter;
  index: number;
  onClick: () => void;
}

export function LetterCard({ letter, index, onClick }: LetterCardProps) {
  const cover = letterCovers[index % letterCovers.length];
  return (
    <button
      onClick={onClick}
      style={{
        textAlign: 'left',
        border: '1px solid rgba(27,27,47,.06)',
        borderRadius: 18,
        overflow: 'hidden',
        cursor: 'pointer',
        background: '#fff',
        padding: 0,
      }}
    >
      <div style={{ height: 96, background: cover, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <span style={{ fontFamily: 'Sora', fontWeight: 800, fontSize: 40, color: 'rgba(255,255,255,.95)' }}>
          {letter.upperCase}
          {letter.lowerCase}
        </span>
      </div>
      <div style={{ padding: '14px 16px 16px' }}>
        <div style={{ fontSize: 22, marginBottom: 6 }}>{letter.exampleEmoji}</div>
        <div style={{ fontFamily: 'Sora', fontWeight: 700, fontSize: 14.5 }}>{letter.exampleWord}</div>
        <div style={{ fontSize: 12, color: '#8A8A99', marginTop: 2 }}>đọc là “{letter.sound}”</div>
      </div>
    </button>
  );
}
```

- [ ] **Step 2: Write `LetterDetail` (modal)**

Create `src/presentation/components/LetterDetail.tsx`:

```tsx
import type { CSSProperties } from 'react';
import type { AlphabetLetter } from '../../domain/models/AlphabetLetter';
import { letterCovers } from '../../styles/tokens';

interface LetterDetailProps {
  letter: AlphabetLetter;
  index: number;
  hasPrev: boolean;
  hasNext: boolean;
  onPrev: () => void;
  onNext: () => void;
  onClose: () => void;
}

export function LetterDetail({ letter, index, hasPrev, hasNext, onPrev, onNext, onClose }: LetterDetailProps) {
  const cover = letterCovers[index % letterCovers.length];
  return (
    <div
      onClick={onClose}
      style={{ position: 'fixed', inset: 0, background: 'rgba(20,10,40,.45)', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 20, zIndex: 50 }}
    >
      <div onClick={(e) => e.stopPropagation()} style={{ width: '100%', maxWidth: 420, background: '#fff', borderRadius: 24, overflow: 'hidden', animation: 'fadeUp .3s ease both' }}>
        <div style={{ background: cover, padding: 34, textAlign: 'center', color: '#fff' }}>
          <div style={{ fontFamily: 'Sora', fontWeight: 800, fontSize: 72, lineHeight: 1 }}>
            {letter.upperCase}
            {letter.lowerCase}
          </div>
          <div style={{ fontSize: 64, marginTop: 8 }}>{letter.exampleEmoji}</div>
        </div>
        <div style={{ padding: '22px 26px 26px', textAlign: 'center' }}>
          <div style={{ fontSize: 12, color: '#8A8A99', textTransform: 'uppercase', letterSpacing: '.06em' }}>Tên chữ</div>
          <div style={{ fontFamily: 'Sora', fontWeight: 700, fontSize: 22, margin: '2px 0 16px' }}>{letter.name}</div>
          <div style={{ display: 'flex', gap: 12, justifyContent: 'center', marginBottom: 18 }}>
            <div style={{ flex: 1, background: '#F4F3FF', borderRadius: 14, padding: 12 }}>
              <div style={{ fontSize: 12, color: '#8A8A99' }}>Cách đọc</div>
              <div style={{ fontFamily: 'Sora', fontWeight: 700, fontSize: 18, color: '#4F46E5' }}>{letter.sound}</div>
            </div>
            <div style={{ flex: 1, background: '#FFF6EE', borderRadius: 14, padding: 12 }}>
              <div style={{ fontSize: 12, color: '#8A8A99' }}>Ví dụ</div>
              <div style={{ fontFamily: 'Sora', fontWeight: 700, fontSize: 18, color: '#FF6B4A' }}>{letter.exampleWord}</div>
            </div>
          </div>
          <div style={{ display: 'flex', gap: 10, justifyContent: 'space-between' }}>
            <button onClick={onPrev} disabled={!hasPrev} style={navBtnStyle(!hasPrev)}>← Trước</button>
            <button onClick={onClose} style={{ flex: 1, border: 'none', borderRadius: 12, background: 'linear-gradient(135deg,#4F46E5,#6D28D9)', color: '#fff', fontWeight: 700, fontFamily: 'Sora', cursor: 'pointer', padding: 12 }}>Đóng</button>
            <button onClick={onNext} disabled={!hasNext} style={navBtnStyle(!hasNext)}>Sau →</button>
          </div>
        </div>
      </div>
    </div>
  );
}

function navBtnStyle(disabled: boolean): CSSProperties {
  return {
    border: '1.5px solid #E4E2EC',
    borderRadius: 12,
    background: '#fff',
    padding: '12px 14px',
    cursor: disabled ? 'default' : 'pointer',
    fontWeight: 600,
    color: disabled ? '#C8C5D6' : '#3A3A4D',
    opacity: disabled ? 0.6 : 1,
  };
}
```

- [ ] **Step 3: Write `AlphabetPage`**

Create `src/presentation/pages/AlphabetPage.tsx`:

```tsx
import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { container } from '../../di/container';
import type { AlphabetLetter } from '../../domain/models/AlphabetLetter';
import { LetterCard } from '../components/LetterCard';
import { LetterDetail } from '../components/LetterDetail';
import { useAuth } from '../auth/useAuth';

type Status = 'loading' | 'ready' | 'error';

export function AlphabetPage() {
  const { session, logout } = useAuth();
  const navigate = useNavigate();
  const [letters, setLetters] = useState<AlphabetLetter[]>([]);
  const [status, setStatus] = useState<Status>('loading');
  const [selected, setSelected] = useState<number | null>(null);

  const load = useCallback(() => {
    setStatus('loading');
    container
      .getAlphabet()
      .then((data) => {
        setLetters(data);
        setStatus('ready');
      })
      .catch(() => setStatus('error'));
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  function handleLogout() {
    logout();
    navigate('/login');
  }

  return (
    <div style={{ minHeight: '100vh', background: '#F4F3EF' }}>
      <header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '22px 34px', position: 'sticky', top: 0, background: 'rgba(244,243,239,.88)', backdropFilter: 'blur(12px)', zIndex: 5 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 11 }}>
          <div style={{ width: 44, height: 44, borderRadius: '50%', background: 'linear-gradient(135deg,#4F46E5,#7C3AED)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: 'Sora', fontWeight: 700, color: '#fff' }}>
            {initials(session?.user.displayName)}
          </div>
          <div>
            <div style={{ fontFamily: 'Sora', fontWeight: 700, fontSize: 14 }}>{session?.user.displayName}</div>
            <div style={{ fontSize: 11.5, color: '#8A8A99' }}>Học viên</div>
          </div>
        </div>
        <button onClick={handleLogout} style={{ border: '1px solid rgba(27,27,47,.08)', background: '#fff', borderRadius: 12, padding: '10px 16px', cursor: 'pointer', fontWeight: 600, fontSize: 13, color: '#5A5A6B' }}>
          Đăng xuất
        </button>
      </header>

      <main style={{ padding: '6px 34px 60px' }}>
        <h1 style={{ fontFamily: 'Sora', fontWeight: 800, fontSize: 26, letterSpacing: '-.02em', margin: '0 0 4px' }}>
          Xin chào, {session?.user.displayName} 👋
        </h1>
        <p style={{ fontSize: 14, color: '#6B6B80', margin: '0 0 24px' }}>Cùng học bảng chữ cái tiếng Việt nhé!</p>

        {status === 'loading' && <p style={{ color: '#6B6B80' }}>Đang tải bảng chữ cái…</p>}

        {status === 'error' && (
          <div style={{ background: '#FEF2F2', border: '1px solid #FECACA', color: '#B91C1C', padding: '12px 14px', borderRadius: 12, display: 'inline-flex', gap: 12, alignItems: 'center' }}>
            Không tải được bảng chữ cái.
            <button onClick={load} style={{ border: 'none', background: '#B91C1C', color: '#fff', borderRadius: 8, padding: '6px 12px', cursor: 'pointer' }}>Thử lại</button>
          </div>
        )}

        {status === 'ready' && (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(150px,1fr))', gap: 16 }}>
            {letters.map((letter, index) => (
              <LetterCard key={letter.id} letter={letter} index={index} onClick={() => setSelected(index)} />
            ))}
          </div>
        )}
      </main>

      {selected !== null && letters[selected] && (
        <LetterDetail
          letter={letters[selected]}
          index={selected}
          hasPrev={selected > 0}
          hasNext={selected < letters.length - 1}
          onPrev={() => setSelected((s) => (s === null ? s : Math.max(0, s - 1)))}
          onNext={() => setSelected((s) => (s === null ? s : Math.min(letters.length - 1, s + 1)))}
          onClose={() => setSelected(null)}
        />
      )}
    </div>
  );
}

function initials(name?: string): string {
  if (!name) {
    return '🙂';
  }
  const parts = name.trim().split(/\s+/);
  const first = parts[0]?.[0] ?? '';
  const last = parts.length > 1 ? parts[parts.length - 1][0] : '';
  return (first + last).toUpperCase();
}
```

- [ ] **Step 4: Type-check + build the whole app**

Run: `npm run build`
Expected: `tsc -b` + `vite build` succeed with no type errors (all pages referenced by the router now exist).

- [ ] **Step 5: Run the frontend tests**

Run: `npm test`
Expected: PASS (use-case + TokenStorage tests still green).

- [ ] **Step 6: Commit**

```bash
git add src/presentation/components/LetterCard.tsx src/presentation/components/LetterDetail.tsx src/presentation/pages/AlphabetPage.tsx
git commit -m "feat: add alphabet learning page with letter cards and detail modal

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task B9: End-to-end verification

**Files:** none (manual verification)

- [ ] **Step 1: Start backend + database**

```bash
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be
docker compose up -d
dotnet run --project src/Web.Api
```
Expected: API listening on `http://localhost:5113`; on Development startup it auto-applies migrations (seed already present, so no-op after the first run).

- [ ] **Step 2: Start the frontend dev server**

```bash
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-web
npm run dev
```
Expected: Vite serves `http://localhost:5173`.

- [ ] **Step 3: Manual smoke test in the browser**

1. Open `http://localhost:5173` → redirected to `/login` (no session).
2. Log in with `demo@dlearning.vn` / `Demo@123` (or username `demo`) → lands on `/alphabet` showing 29 colourful letter cards.
3. Click a card → detail modal shows the letter (upper+lower), tên chữ, cách đọc, từ ví dụ + emoji; Trước/Sau navigate; Đóng closes.
4. Click "Đăng xuất" → back to `/login`; visiting `/alphabet` directly now redirects to `/login`.
5. From `/login` click "Đăng ký ngay", create a new account → auto-logged-in and taken to `/alphabet`.
6. Wrong password on login shows the red error banner.

- [ ] **Step 4: Stop the servers** (Ctrl-C both) when verification is complete.

---

# Self-Review Notes (coverage map)

- **Spec §3.1 (template, drop Examples, keep tests, packages)** → A1, A3.
- **Spec §3.2 (Users slice: register/login, ports, JWT, PBKDF2, 401)** → A3 (Unauthorized), A4 (User), A5 (ports), A6 (register), A7 (login), A9 (hasher/JWT/config), A10 (DI/JWT), A11 (endpoints).
- **Spec §3.3 (Alphabets slice)** → A8 (entity/query), A9 (config+seed), A11 (endpoint).
- **Spec §3.4 (29-letter seed)** → A9 Step 4.
- **Spec §3.5 (Docker + migration + dev auto-migrate)** → A2, A10 (MigrationExtensions), A12.
- **Spec §3.6 (tests)** → A4/A6/A7/A8 (unit), A13 (integration), ArchitectureTests kept (A1/A10).
- **Spec §4 (FE clean architecture + screens + auth + errors + tests)** → B1–B9.
- **Password hashing decision (PBKDF2, native, arch-test-safe)** → Global Constraints + A9; overrides the spec's earlier "BCrypt" wording.
