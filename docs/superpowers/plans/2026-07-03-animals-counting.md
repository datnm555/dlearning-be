# dLearning — Animals + Counting Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the last two preschool "coming soon" products into working lessons — JWT APIs `GET /animals` (12 localized animals + sounds) and `GET /counting` (1–10 with localized words), each with its own learning screen; all data flows FE → API → PostgreSQL.

**Architecture:** Two backend vertical slices that are structural twins of the `Colors` slice: language-neutral reference entities seeded in the DB, `.resx`-localized display strings resolved in the Web.Api endpoints, one migration creating both tables and flipping both products available. The frontend adds two clean-arch stacks (model → port → use case → API repo → page) mirroring `ColorsPage`; the home menu needs no change because `openProduct` already routes available products to `/<code>`.

**Tech Stack:** .NET 10, EF Core 10, `Microsoft.Extensions.Localization`, Vite + React 19 + TypeScript, Vitest, Docker Compose.

## Global Constraints

- **Backend repo:** `/Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be` (`main`). **Frontend repo:** `/Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-web` (`main`). Commits authored as `datnm555 <mydatng@gmail.com>` (already configured); keep the `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>` trailer.
- **Central Package Management**; no new NuGet packages. **`TreatWarningsAsErrors` + `AnalysisMode=All`** — style/analyzer violations fail the build; file-scoped namespaces; `CultureInfo.InvariantCulture` for numeric formatting in seeds and key building.
- **Dependency rule** enforced by ArchitectureTests: Application has no ASP.NET/localization dependency; handlers `internal sealed` implementing `IQueryHandler<,>`; reference entities `builder.Ignore(x => x.DomainEvents)`.
- **EF migrations** in `src/Infrastructure/Database/Migrations/` (`-o Database/Migrations`), run with `ASPNETCORE_ENVIRONMENT=Production`; use the BE repo's scratch postgres (`docker compose up -d` … `down`) — the deploy stack DB stays untouched and migrates itself on rebuild.
- **Cultures `en`/`vi`, default `vi`** via existing request localization (`?lang=` then `Accept-Language`).
- **Animal codes (order 1–12):** `cat, dog, cow, chicken, duck, pig, elephant, lion, monkey, bird, fish, frog`. Seed GUIDs `a11a0000-0000-4000-8000-0000000000NN` (NN = order hex 01..0c).
- **Counting values 1–10**, seed GUIDs `c0117000-0000-4000-8000-0000000000NN` (NN = value hex 01..0a).
- **API contract:** `GET /animals?lang=` → `[{ code, name, emoji, sound, displayOrder }]`; `GET /counting?lang=` → `[{ value, word, emoji }]`; both JWT-protected. `/api` proxy unchanged.
- **Frontend:** `erasableSyntaxOnly` ON (no ctor parameter properties); `presentation → application → domain`, `infrastructure → domain`, `di` composes; pass `lang` from `useI18n()`.

---

# Phase A — Backend (`dlearning-be`)

## Task A1: `Animal` + `CountingNumber` entities

**Files:**
- Create: `src/Domain/Animals/Animal.cs`, `src/Domain/Counting/CountingNumber.cs`
- Test: `tests/Application.UnitTests/Animals/AnimalTests.cs`

**Interfaces:**
- Produces: `Animal : Entity` (`Code`, `Emoji`, `DisplayOrder` — init props); `CountingNumber : Entity` (`Value`, `Emoji` — init props).

- [ ] **Step 1: Write the failing test**

Create `tests/Application.UnitTests/Animals/AnimalTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~AnimalTests"`
Expected: FAIL (build error — types don't exist).

- [ ] **Step 3: Write the entities**

Create `src/Domain/Animals/Animal.cs`:

```csharp
using SharedKernel;

namespace Domain.Animals;

public sealed class Animal : Entity
{
    public string Code { get; init; } = string.Empty;

    public string Emoji { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }
}
```

Create `src/Domain/Counting/CountingNumber.cs`:

```csharp
using SharedKernel;

namespace Domain.Counting;

public sealed class CountingNumber : Entity
{
    public int Value { get; init; }

    public string Emoji { get; init; } = string.Empty;
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~AnimalTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be
git add src/Domain/Animals src/Domain/Counting tests/Application.UnitTests/Animals/AnimalTests.cs
git commit -m "feat: add Animal and CountingNumber domain entities

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A2: Queries + handlers + DbSets + DI

**Files:**
- Create: `src/Application/Animals/Data/AnimalDto.cs`, `src/Application/Animals/GetAnimalsQuery.cs`, `src/Application/Animals/GetAnimalsQueryHandler.cs`
- Create: `src/Application/Counting/Data/CountingNumberDto.cs`, `src/Application/Counting/GetCountingQuery.cs`, `src/Application/Counting/GetCountingQueryHandler.cs`
- Modify: `src/Application/Abstractions/Data/IApplicationDbContext.cs`, `src/Infrastructure/Database/ApplicationDbContext.cs`, `src/Application/DependencyInjection.cs`
- Test: `tests/Application.UnitTests/Animals/GetAnimalsQueryHandlerTests.cs`, `tests/Application.UnitTests/Counting/GetCountingQueryHandlerTests.cs`

**Interfaces:**
- Produces: `AnimalDto(string Code, string Emoji, int DisplayOrder)`; `GetAnimalsQuery() : IQuery<IReadOnlyList<AnimalDto>>` (ordered by `DisplayOrder`); `CountingNumberDto(int Value, string Emoji)`; `GetCountingQuery() : IQuery<IReadOnlyList<CountingNumberDto>>` (ordered by `Value`); `IApplicationDbContext.Animals`, `.CountingNumbers`.

- [ ] **Step 1: Write the failing tests**

Create `tests/Application.UnitTests/Animals/GetAnimalsQueryHandlerTests.cs`:

```csharp
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
```

Create `tests/Application.UnitTests/Counting/GetCountingQueryHandlerTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~GetAnimalsQueryHandlerTests|FullyQualifiedName~GetCountingQueryHandlerTests"`
Expected: FAIL (build errors).

- [ ] **Step 3: Write DTOs, queries, handlers**

Create `src/Application/Animals/Data/AnimalDto.cs`:

```csharp
namespace Application.Animals.Data;

public sealed record AnimalDto(string Code, string Emoji, int DisplayOrder);
```

Create `src/Application/Animals/GetAnimalsQuery.cs`:

```csharp
using Application.Abstractions.Messaging;
using Application.Animals.Data;

namespace Application.Animals;

public sealed record GetAnimalsQuery : IQuery<IReadOnlyList<AnimalDto>>;
```

Create `src/Application/Animals/GetAnimalsQueryHandler.cs`:

```csharp
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Animals.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Animals;

internal sealed class GetAnimalsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAnimalsQuery, IReadOnlyList<AnimalDto>>
{
    public async Task<Result<IReadOnlyList<AnimalDto>>> Handle(
        GetAnimalsQuery query,
        CancellationToken cancellationToken)
    {
        List<AnimalDto> animals = await dbContext.Animals
            .OrderBy(a => a.DisplayOrder)
            .Select(a => new AnimalDto(a.Code, a.Emoji, a.DisplayOrder))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AnimalDto>>(animals);
    }
}
```

Create `src/Application/Counting/Data/CountingNumberDto.cs`:

```csharp
namespace Application.Counting.Data;

public sealed record CountingNumberDto(int Value, string Emoji);
```

Create `src/Application/Counting/GetCountingQuery.cs`:

```csharp
using Application.Abstractions.Messaging;
using Application.Counting.Data;

namespace Application.Counting;

public sealed record GetCountingQuery : IQuery<IReadOnlyList<CountingNumberDto>>;
```

Create `src/Application/Counting/GetCountingQueryHandler.cs`:

```csharp
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Counting.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Counting;

internal sealed class GetCountingQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetCountingQuery, IReadOnlyList<CountingNumberDto>>
{
    public async Task<Result<IReadOnlyList<CountingNumberDto>>> Handle(
        GetCountingQuery query,
        CancellationToken cancellationToken)
    {
        List<CountingNumberDto> numbers = await dbContext.CountingNumbers
            .OrderBy(n => n.Value)
            .Select(n => new CountingNumberDto(n.Value, n.Emoji))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CountingNumberDto>>(numbers);
    }
}
```

- [ ] **Step 4: Expose DbSets and register handlers**

`src/Application/Abstractions/Data/IApplicationDbContext.cs` — add usings `Domain.Animals;`, `Domain.Counting;` and after `Colors`:

```csharp
    DbSet<Animal> Animals { get; }

    DbSet<CountingNumber> CountingNumbers { get; }
```

`src/Infrastructure/Database/ApplicationDbContext.cs` — add the same usings and after `Colors`:

```csharp
    public DbSet<Animal> Animals => Set<Animal>();

    public DbSet<CountingNumber> CountingNumbers => Set<CountingNumber>();
```

`src/Application/DependencyInjection.cs` — add usings `Application.Animals;`, `Application.Animals.Data;`, `Application.Counting;`, `Application.Counting.Data;` and register:

```csharp
        services.AddScoped<IQueryHandler<GetAnimalsQuery, IReadOnlyList<AnimalDto>>, GetAnimalsQueryHandler>();
        services.AddScoped<IQueryHandler<GetCountingQuery, IReadOnlyList<CountingNumberDto>>, GetCountingQueryHandler>();
```

- [ ] **Step 5: Run to verify they pass**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~Animals|FullyQualifiedName~Counting"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Application/Animals src/Application/Counting src/Application/Abstractions/Data/IApplicationDbContext.cs src/Infrastructure/Database/ApplicationDbContext.cs src/Application/DependencyInjection.cs tests/Application.UnitTests/Animals/GetAnimalsQueryHandlerTests.cs tests/Application.UnitTests/Counting
git commit -m "feat: add GetAnimals and GetCounting queries with DbSets

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A3: EF configs + seeds + product flips + migration

**Files:**
- Create: `src/Infrastructure/Animals/AnimalConfiguration.cs`, `src/Infrastructure/Counting/CountingNumberConfiguration.cs`
- Modify: `src/Infrastructure/Catalog/ProductConfiguration.cs` (flip `animals` + `counting` → `true`)
- Create: `src/Infrastructure/Database/Migrations/*_AnimalsAndCounting.cs` (generated)

- [ ] **Step 1: Write the animal configuration**

Create `src/Infrastructure/Animals/AnimalConfiguration.cs`:

```csharp
using System.Globalization;
using Domain.Animals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Animals;

internal sealed class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> builder)
    {
        builder.ToTable("animals");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Code).HasMaxLength(32).IsRequired();
        builder.Property(a => a.Emoji).HasMaxLength(16).IsRequired();
        builder.Property(a => a.DisplayOrder).IsRequired();

        builder.HasIndex(a => a.Code).IsUnique();
        builder.Ignore(a => a.DomainEvents);

        builder.HasData(Seed());
    }

    private static object[] Seed()
    {
        (string Code, string Emoji)[] rows =
        [
            ("cat", "🐱"),
            ("dog", "🐶"),
            ("cow", "🐄"),
            ("chicken", "🐔"),
            ("duck", "🦆"),
            ("pig", "🐷"),
            ("elephant", "🐘"),
            ("lion", "🦁"),
            ("monkey", "🐵"),
            ("bird", "🐦"),
            ("fish", "🐟"),
            ("frog", "🐸")
        ];

        var seed = new object[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            int order = i + 1;
            string hex = order.ToString("x2", CultureInfo.InvariantCulture);
            seed[i] = new
            {
                Id = new Guid("a11a0000-0000-4000-8000-0000000000" + hex),
                Code = rows[i].Code,
                Emoji = rows[i].Emoji,
                DisplayOrder = order
            };
        }

        return seed;
    }
}
```

- [ ] **Step 2: Write the counting configuration**

Create `src/Infrastructure/Counting/CountingNumberConfiguration.cs`:

```csharp
using System.Globalization;
using Domain.Counting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Counting;

internal sealed class CountingNumberConfiguration : IEntityTypeConfiguration<CountingNumber>
{
    public void Configure(EntityTypeBuilder<CountingNumber> builder)
    {
        builder.ToTable("counting_numbers");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Value).IsRequired();
        builder.Property(n => n.Emoji).HasMaxLength(16).IsRequired();

        builder.HasIndex(n => n.Value).IsUnique();
        builder.Ignore(n => n.DomainEvents);

        builder.HasData(Seed());
    }

    private static object[] Seed()
    {
        string[] emojis = ["🍎", "🍌", "🐟", "🌸", "⭐", "🍇", "🐞", "🐙", "🎈", "🍭"];

        var seed = new object[emojis.Length];
        for (int i = 0; i < emojis.Length; i++)
        {
            int value = i + 1;
            string hex = value.ToString("x2", CultureInfo.InvariantCulture);
            seed[i] = new
            {
                Id = new Guid("c0117000-0000-4000-8000-0000000000" + hex),
                Value = value,
                Emoji = emojis[i]
            };
        }

        return seed;
    }
}
```

- [ ] **Step 3: Flip both products to available**

In `src/Infrastructure/Catalog/ProductConfiguration.cs`, the seed rows become:

```csharp
            ("alphabet", "🔤", true),
            ("animals", "🐾", true),
            ("colors", "🎨", true),
            ("counting", "🔢", true)
```

(`animals` and `counting` change `false` → `true`.)

- [ ] **Step 4: Build, then create + apply the migration on the scratch DB**

```bash
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be
dotnet build DLearning.slnx
docker compose up -d
dotnet tool restore
ASPNETCORE_ENVIRONMENT=Production dotnet ef migrations add AnimalsAndCounting --project src/Infrastructure --startup-project src/Web.Api -o Database/Migrations
ASPNETCORE_ENVIRONMENT=Production dotnet ef database update --project src/Infrastructure --startup-project src/Web.Api
```
Expected: build succeeds; migration `Up()` creates `animals` (12 inserts) + `counting_numbers` (10 inserts) + two `UpdateData` ops on `products`.

- [ ] **Step 5: Verify, then stop the scratch DB**

```bash
docker exec dlearning-postgres psql -U postgres -d dlearning -t -c 'select count(*) from animals;'          # 12
docker exec dlearning-postgres psql -U postgres -d dlearning -t -c 'select count(*) from counting_numbers;' # 10
docker exec dlearning-postgres psql -U postgres -d dlearning -t -c 'select "Code","IsAvailable" from products order by "DisplayOrder";'  # all t except none — 4 rows all true
docker compose down
```

- [ ] **Step 6: Commit**

```bash
git add src/Infrastructure/Animals src/Infrastructure/Counting src/Infrastructure/Catalog/ProductConfiguration.cs src/Infrastructure/Database/Migrations
git commit -m "feat: add animals + counting EF configs, seeds, and product flips

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A4: resx keys (animals names/sounds + number words)

**Files:**
- Modify: `src/Web.Api/Resources/SharedResource.en.resx`, `src/Web.Api/Resources/SharedResource.vi.resx`

- [ ] **Step 1: Add English keys** (before `</root>`):

```xml
  <data name="Animal.cat" xml:space="preserve"><value>Cat</value></data>
  <data name="Animal.dog" xml:space="preserve"><value>Dog</value></data>
  <data name="Animal.cow" xml:space="preserve"><value>Cow</value></data>
  <data name="Animal.chicken" xml:space="preserve"><value>Chicken</value></data>
  <data name="Animal.duck" xml:space="preserve"><value>Duck</value></data>
  <data name="Animal.pig" xml:space="preserve"><value>Pig</value></data>
  <data name="Animal.elephant" xml:space="preserve"><value>Elephant</value></data>
  <data name="Animal.lion" xml:space="preserve"><value>Lion</value></data>
  <data name="Animal.monkey" xml:space="preserve"><value>Monkey</value></data>
  <data name="Animal.bird" xml:space="preserve"><value>Bird</value></data>
  <data name="Animal.fish" xml:space="preserve"><value>Fish</value></data>
  <data name="Animal.frog" xml:space="preserve"><value>Frog</value></data>
  <data name="AnimalSound.cat" xml:space="preserve"><value>meow</value></data>
  <data name="AnimalSound.dog" xml:space="preserve"><value>woof</value></data>
  <data name="AnimalSound.cow" xml:space="preserve"><value>moo</value></data>
  <data name="AnimalSound.chicken" xml:space="preserve"><value>cluck</value></data>
  <data name="AnimalSound.duck" xml:space="preserve"><value>quack</value></data>
  <data name="AnimalSound.pig" xml:space="preserve"><value>oink</value></data>
  <data name="AnimalSound.elephant" xml:space="preserve"><value>toot</value></data>
  <data name="AnimalSound.lion" xml:space="preserve"><value>roar</value></data>
  <data name="AnimalSound.monkey" xml:space="preserve"><value>ooh ooh</value></data>
  <data name="AnimalSound.bird" xml:space="preserve"><value>tweet</value></data>
  <data name="AnimalSound.fish" xml:space="preserve"><value>blub</value></data>
  <data name="AnimalSound.frog" xml:space="preserve"><value>ribbit</value></data>
  <data name="Number.1" xml:space="preserve"><value>One</value></data>
  <data name="Number.2" xml:space="preserve"><value>Two</value></data>
  <data name="Number.3" xml:space="preserve"><value>Three</value></data>
  <data name="Number.4" xml:space="preserve"><value>Four</value></data>
  <data name="Number.5" xml:space="preserve"><value>Five</value></data>
  <data name="Number.6" xml:space="preserve"><value>Six</value></data>
  <data name="Number.7" xml:space="preserve"><value>Seven</value></data>
  <data name="Number.8" xml:space="preserve"><value>Eight</value></data>
  <data name="Number.9" xml:space="preserve"><value>Nine</value></data>
  <data name="Number.10" xml:space="preserve"><value>Ten</value></data>
```

- [ ] **Step 2: Add Vietnamese keys** (before `</root>`):

```xml
  <data name="Animal.cat" xml:space="preserve"><value>Mèo</value></data>
  <data name="Animal.dog" xml:space="preserve"><value>Chó</value></data>
  <data name="Animal.cow" xml:space="preserve"><value>Bò</value></data>
  <data name="Animal.chicken" xml:space="preserve"><value>Gà</value></data>
  <data name="Animal.duck" xml:space="preserve"><value>Vịt</value></data>
  <data name="Animal.pig" xml:space="preserve"><value>Lợn</value></data>
  <data name="Animal.elephant" xml:space="preserve"><value>Voi</value></data>
  <data name="Animal.lion" xml:space="preserve"><value>Sư tử</value></data>
  <data name="Animal.monkey" xml:space="preserve"><value>Khỉ</value></data>
  <data name="Animal.bird" xml:space="preserve"><value>Chim</value></data>
  <data name="Animal.fish" xml:space="preserve"><value>Cá</value></data>
  <data name="Animal.frog" xml:space="preserve"><value>Ếch</value></data>
  <data name="AnimalSound.cat" xml:space="preserve"><value>meo meo</value></data>
  <data name="AnimalSound.dog" xml:space="preserve"><value>gâu gâu</value></data>
  <data name="AnimalSound.cow" xml:space="preserve"><value>ò ọ</value></data>
  <data name="AnimalSound.chicken" xml:space="preserve"><value>cục tác</value></data>
  <data name="AnimalSound.duck" xml:space="preserve"><value>cạp cạp</value></data>
  <data name="AnimalSound.pig" xml:space="preserve"><value>ụt ịt</value></data>
  <data name="AnimalSound.elephant" xml:space="preserve"><value>ù ù</value></data>
  <data name="AnimalSound.lion" xml:space="preserve"><value>gừ gừ</value></data>
  <data name="AnimalSound.monkey" xml:space="preserve"><value>khẹc khẹc</value></data>
  <data name="AnimalSound.bird" xml:space="preserve"><value>chíp chíp</value></data>
  <data name="AnimalSound.fish" xml:space="preserve"><value>ục ục</value></data>
  <data name="AnimalSound.frog" xml:space="preserve"><value>ộp ộp</value></data>
  <data name="Number.1" xml:space="preserve"><value>Một</value></data>
  <data name="Number.2" xml:space="preserve"><value>Hai</value></data>
  <data name="Number.3" xml:space="preserve"><value>Ba</value></data>
  <data name="Number.4" xml:space="preserve"><value>Bốn</value></data>
  <data name="Number.5" xml:space="preserve"><value>Năm</value></data>
  <data name="Number.6" xml:space="preserve"><value>Sáu</value></data>
  <data name="Number.7" xml:space="preserve"><value>Bảy</value></data>
  <data name="Number.8" xml:space="preserve"><value>Tám</value></data>
  <data name="Number.9" xml:space="preserve"><value>Chín</value></data>
  <data name="Number.10" xml:space="preserve"><value>Mười</value></data>
```

- [ ] **Step 3: Build + commit**

```bash
dotnet build DLearning.slnx
git add src/Web.Api/Resources
git commit -m "feat: add en/vi resx keys for animals and counting

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A5: Endpoints `GET /animals` + `GET /counting`

**Files:**
- Create: `src/Web.Api/Endpoints/Animals/GetAnimals.cs`, `src/Web.Api/Endpoints/Counting/GetCounting.cs`

- [ ] **Step 1: Write the animals endpoint**

Create `src/Web.Api/Endpoints/Animals/GetAnimals.cs`:

```csharp
using Application.Abstractions.Messaging;
using Application.Animals;
using Application.Animals.Data;
using Microsoft.Extensions.Localization;
using SharedKernel;
using Web.Api.Infrastructure;
using Web.Api.Middleware;

namespace Web.Api.Endpoints.Animals;

internal sealed class GetAnimals : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/animals", async (
            IQueryHandler<GetAnimalsQuery, IReadOnlyList<AnimalDto>> handler,
            IStringLocalizer<SharedResource> localizer,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<AnimalDto>> result =
                await handler.Handle(new GetAnimalsQuery(), cancellationToken);

            return result.ToHttpResult(animals => Results.Ok(
                animals.Select(a => new
                {
                    a.Code,
                    name = localizer["Animal." + a.Code].Value,
                    a.Emoji,
                    sound = localizer["AnimalSound." + a.Code].Value,
                    a.DisplayOrder
                })));
        })
        .RequireAuthorization();
    }
}
```

- [ ] **Step 2: Write the counting endpoint**

Create `src/Web.Api/Endpoints/Counting/GetCounting.cs`:

```csharp
using System.Globalization;
using Application.Abstractions.Messaging;
using Application.Counting;
using Application.Counting.Data;
using Microsoft.Extensions.Localization;
using SharedKernel;
using Web.Api.Infrastructure;
using Web.Api.Middleware;

namespace Web.Api.Endpoints.Counting;

internal sealed class GetCounting : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/counting", async (
            IQueryHandler<GetCountingQuery, IReadOnlyList<CountingNumberDto>> handler,
            IStringLocalizer<SharedResource> localizer,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<CountingNumberDto>> result =
                await handler.Handle(new GetCountingQuery(), cancellationToken);

            return result.ToHttpResult(numbers => Results.Ok(
                numbers.Select(n => new
                {
                    n.Value,
                    word = localizer["Number." + n.Value.ToString(CultureInfo.InvariantCulture)].Value,
                    n.Emoji
                })));
        })
        .RequireAuthorization();
    }
}
```

- [ ] **Step 3: Build + smoke test**

```bash
dotnet build DLearning.slnx
docker compose up -d
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Web.Api &
# wait, then:
TOKEN=$(curl -s -X POST http://localhost:5113/users/login -H 'Content-Type: application/json' -d '{"identifier":"demo","password":"Demo@123"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
curl -s -o /dev/null -w "animals no-token: %{http_code}\n" http://localhost:5113/animals              # 401
curl -s "http://localhost:5113/animals?lang=vi" -H "Authorization: Bearer $TOKEN" | grep -o '"name":"[^"]*"' | head -1   # "Mèo"
curl -s "http://localhost:5113/animals?lang=en" -H "Authorization: Bearer $TOKEN" | grep -o '"sound":"[^"]*"' | head -1  # "meow"
curl -s "http://localhost:5113/counting?lang=vi" -H "Authorization: Bearer $TOKEN" | grep -o '"word":"[^"]*"' | head -1  # "Một"
curl -s "http://localhost:5113/counting?lang=en" -H "Authorization: Bearer $TOKEN" | grep -o '"value"' | wc -l           # 10
pkill -f Web.Api; docker compose down
```

- [ ] **Step 4: Commit**

```bash
git add src/Web.Api/Endpoints/Animals src/Web.Api/Endpoints/Counting
git commit -m "feat: add JWT-protected localized animals and counting endpoints

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A6: Integration tests + catalog availability update

**Files:**
- Create: `tests/Api.IntegrationTests/Animals/AnimalsEndpointsTests.cs`, `tests/Api.IntegrationTests/Counting/CountingEndpointsTests.cs`
- Modify: `tests/Api.IntegrationTests/Catalog/CatalogEndpointsTests.cs` (available count 2 → 4)

- [ ] **Step 1: Write the animals integration tests**

Create `tests/Api.IntegrationTests/Animals/AnimalsEndpointsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Api.IntegrationTests.Animals;

public sealed class AnimalsEndpointsTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record TokenOnly(string Token);
    private sealed record AnimalView(string Code, string Name, string Emoji, string Sound, int DisplayOrder);

    private async Task<string> LoginAsync()
    {
        var login = await _client.PostAsJsonAsync("/users/login",
            new { identifier = "demo", password = "Demo@123" });
        var auth = await login.Content.ReadFromJsonAsync<TokenOnly>();
        auth.ShouldNotBeNull();
        return auth.Token;
    }

    [Fact]
    public async Task GetAnimals_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/animals");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAnimals_En_Returns12LocalizedAnimals()
    {
        var token = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/animals?lang=en");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var animals = await response.Content.ReadFromJsonAsync<List<AnimalView>>();
        animals.ShouldNotBeNull();
        animals.Count.ShouldBe(12);
        animals[0].Code.ShouldBe("cat");
        animals[0].Name.ShouldBe("Cat");
        animals[0].Sound.ShouldBe("meow");
        animals[^1].Code.ShouldBe("frog");
    }

    [Fact]
    public async Task GetAnimals_Vi_ReturnsVietnameseNamesAndSounds()
    {
        var token = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/animals?lang=vi");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        var animals = await response.Content.ReadFromJsonAsync<List<AnimalView>>();
        animals.ShouldNotBeNull();
        animals[0].Name.ShouldBe("Mèo");
        animals[0].Sound.ShouldBe("meo meo");
    }
}
```

- [ ] **Step 2: Write the counting integration tests**

Create `tests/Api.IntegrationTests/Counting/CountingEndpointsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Api.IntegrationTests.Counting;

public sealed class CountingEndpointsTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record TokenOnly(string Token);
    private sealed record NumberView(int Value, string Word, string Emoji);

    private async Task<string> LoginAsync()
    {
        var login = await _client.PostAsJsonAsync("/users/login",
            new { identifier = "demo", password = "Demo@123" });
        var auth = await login.Content.ReadFromJsonAsync<TokenOnly>();
        auth.ShouldNotBeNull();
        return auth.Token;
    }

    [Fact]
    public async Task GetCounting_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/counting");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCounting_Returns10NumbersInOrder_Localized()
    {
        var token = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/counting?lang=vi");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var numbers = await response.Content.ReadFromJsonAsync<List<NumberView>>();
        numbers.ShouldNotBeNull();
        numbers.Count.ShouldBe(10);
        numbers[0].Value.ShouldBe(1);
        numbers[0].Word.ShouldBe("Một");
        numbers[^1].Value.ShouldBe(10);
        numbers[^1].Word.ShouldBe("Mười");
    }
}
```

- [ ] **Step 3: Update the catalog availability assertion**

In `tests/Api.IntegrationTests/Catalog/CatalogEndpointsTests.cs`, the availability lines become:

```csharp
        // All four preschool lessons are now available.
        products.Count(p => p.IsAvailable).ShouldBe(4);
        products.Single(p => p.Code == "colors").IsAvailable.ShouldBeTrue();
```

- [ ] **Step 4: Run the full backend suite**

Run: `dotnet test`
Expected: all three projects PASS.

- [ ] **Step 5: Commit**

```bash
git add tests/Api.IntegrationTests
git commit -m "test: add animals + counting integration tests; all four products available

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

# Phase B — Frontend (`dlearning-web`)

## Task B1: Models, ports, use cases (+ tests)

**Files:**
- Create: `src/domain/models/Animal.ts`, `src/domain/models/CountingNumber.ts`
- Create: `src/domain/repositories/AnimalRepository.ts`, `src/domain/repositories/CountingRepository.ts`
- Create: `src/application/usecases/getAnimals.ts` (+ `getAnimals.test.ts`), `src/application/usecases/getCounting.ts` (+ `getCounting.test.ts`)

**Interfaces:**
- Produces: `Animal { code, name, emoji, sound, displayOrder }`; `CountingNumber { value, word, emoji }`; `AnimalRepository.getAnimals(lang)`, `CountingRepository.getCounting(lang)`; `makeGetAnimals(repo)`, `makeGetCounting(repo)`.

- [ ] **Step 1: Write the failing tests**

`src/application/usecases/getAnimals.test.ts`:

```ts
import { describe, it, expect, vi } from 'vitest';
import { makeGetAnimals } from './getAnimals';
import type { AnimalRepository } from '../../domain/repositories/AnimalRepository';

describe('getAnimals use case', () => {
  it('passes the language through to the repository', async () => {
    const animals = [{ code: 'cat', name: 'Mèo', emoji: '🐱', sound: 'meo meo', displayOrder: 1 }];
    const repo: AnimalRepository = { getAnimals: vi.fn().mockResolvedValue(animals) };

    const getAnimals = makeGetAnimals(repo);
    const result = await getAnimals('vi');

    expect(repo.getAnimals).toHaveBeenCalledWith('vi');
    expect(result).toEqual(animals);
  });
});
```

`src/application/usecases/getCounting.test.ts`:

```ts
import { describe, it, expect, vi } from 'vitest';
import { makeGetCounting } from './getCounting';
import type { CountingRepository } from '../../domain/repositories/CountingRepository';

describe('getCounting use case', () => {
  it('passes the language through to the repository', async () => {
    const numbers = [{ value: 1, word: 'Một', emoji: '🍎' }];
    const repo: CountingRepository = { getCounting: vi.fn().mockResolvedValue(numbers) };

    const getCounting = makeGetCounting(repo);
    const result = await getCounting('vi');

    expect(repo.getCounting).toHaveBeenCalledWith('vi');
    expect(result).toEqual(numbers);
  });
});
```

- [ ] **Step 2: Run to verify they fail**

Run: `npm test -- getAnimals getCounting`
Expected: FAIL (missing modules).

- [ ] **Step 3: Write models, ports, use cases**

`src/domain/models/Animal.ts`:

```ts
export interface Animal {
  code: string;
  name: string;
  emoji: string;
  sound: string;
  displayOrder: number;
}
```

`src/domain/models/CountingNumber.ts`:

```ts
export interface CountingNumber {
  value: number;
  word: string;
  emoji: string;
}
```

`src/domain/repositories/AnimalRepository.ts`:

```ts
import type { Animal } from '../models/Animal';
import type { Lang } from './CatalogRepository';

export interface AnimalRepository {
  getAnimals(lang: Lang): Promise<Animal[]>;
}
```

`src/domain/repositories/CountingRepository.ts`:

```ts
import type { CountingNumber } from '../models/CountingNumber';
import type { Lang } from './CatalogRepository';

export interface CountingRepository {
  getCounting(lang: Lang): Promise<CountingNumber[]>;
}
```

`src/application/usecases/getAnimals.ts`:

```ts
import type { Animal } from '../../domain/models/Animal';
import type { AnimalRepository } from '../../domain/repositories/AnimalRepository';
import type { Lang } from '../../domain/repositories/CatalogRepository';

export function makeGetAnimals(repo: AnimalRepository) {
  return (lang: Lang): Promise<Animal[]> => repo.getAnimals(lang);
}
```

`src/application/usecases/getCounting.ts`:

```ts
import type { CountingNumber } from '../../domain/models/CountingNumber';
import type { CountingRepository } from '../../domain/repositories/CountingRepository';
import type { Lang } from '../../domain/repositories/CatalogRepository';

export function makeGetCounting(repo: CountingRepository) {
  return (lang: Lang): Promise<CountingNumber[]> => repo.getCounting(lang);
}
```

- [ ] **Step 4: Run to verify they pass, commit**

```bash
npm test -- getAnimals getCounting
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-web
git add src/domain/models/Animal.ts src/domain/models/CountingNumber.ts src/domain/repositories/AnimalRepository.ts src/domain/repositories/CountingRepository.ts src/application/usecases/getAnimals.ts src/application/usecases/getAnimals.test.ts src/application/usecases/getCounting.ts src/application/usecases/getCounting.test.ts
git commit -m "feat: add animal and counting models, ports, and use cases

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task B2: API repositories + DI

**Files:**
- Create: `src/infrastructure/repositories/ApiAnimalRepository.ts`, `src/infrastructure/repositories/ApiCountingRepository.ts`
- Modify: `src/di/container.ts`

- [ ] **Step 1: Write the repositories**

`src/infrastructure/repositories/ApiAnimalRepository.ts`:

```ts
import type { Animal } from '../../domain/models/Animal';
import type { AnimalRepository } from '../../domain/repositories/AnimalRepository';
import type { Lang } from '../../domain/repositories/CatalogRepository';
import type { HttpClient } from '../http/HttpClient';

export class ApiAnimalRepository implements AnimalRepository {
  private readonly http: HttpClient;

  constructor(http: HttpClient) {
    this.http = http;
  }

  getAnimals(lang: Lang): Promise<Animal[]> {
    return this.http.get<Animal[]>('/animals', { lang });
  }
}
```

`src/infrastructure/repositories/ApiCountingRepository.ts`:

```ts
import type { CountingNumber } from '../../domain/models/CountingNumber';
import type { CountingRepository } from '../../domain/repositories/CountingRepository';
import type { Lang } from '../../domain/repositories/CatalogRepository';
import type { HttpClient } from '../http/HttpClient';

export class ApiCountingRepository implements CountingRepository {
  private readonly http: HttpClient;

  constructor(http: HttpClient) {
    this.http = http;
  }

  getCounting(lang: Lang): Promise<CountingNumber[]> {
    return this.http.get<CountingNumber[]>('/counting', { lang });
  }
}
```

- [ ] **Step 2: Wire the container**

In `src/di/container.ts` add imports:

```ts
import { makeGetAnimals } from '../application/usecases/getAnimals';
import { makeGetCounting } from '../application/usecases/getCounting';
import { ApiAnimalRepository } from '../infrastructure/repositories/ApiAnimalRepository';
import { ApiCountingRepository } from '../infrastructure/repositories/ApiCountingRepository';
```

After `colorRepository`:

```ts
  const animalRepository = new ApiAnimalRepository(http);
  const countingRepository = new ApiCountingRepository(http);
```

In the returned object:

```ts
    getAnimals: makeGetAnimals(animalRepository),
    getCounting: makeGetCounting(countingRepository),
```

- [ ] **Step 3: Type-check + commit**

```bash
npx tsc -b && npm test
git add src/infrastructure/repositories/ApiAnimalRepository.ts src/infrastructure/repositories/ApiCountingRepository.ts src/di/container.ts
git commit -m "feat: add ApiAnimalRepository and ApiCountingRepository, wire use cases

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task B3: AnimalCard/AnimalDetail + NumberCard/NumberDetail

**Files:**
- Create: `src/presentation/components/AnimalCard.tsx`, `AnimalDetail.tsx`, `NumberCard.tsx`, `NumberDetail.tsx`

- [ ] **Step 1: Animal components**

`src/presentation/components/AnimalCard.tsx`:

```tsx
import type { Animal } from '../../domain/models/Animal';
import { letterCovers } from '../../styles/tokens';

interface AnimalCardProps {
  animal: Animal;
  index: number;
  onClick: () => void;
}

export function AnimalCard({ animal, index, onClick }: AnimalCardProps) {
  const cover = letterCovers[index % letterCovers.length];
  return (
    <button
      onClick={onClick}
      style={{ textAlign: 'left', border: '1px solid rgba(27,27,47,.06)', borderRadius: 18, overflow: 'hidden', cursor: 'pointer', background: '#fff', padding: 0 }}
    >
      <div style={{ height: 96, background: cover, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 48 }}>
        {animal.emoji}
      </div>
      <div style={{ padding: '14px 16px 16px' }}>
        <div style={{ fontFamily: 'Sora', fontWeight: 700, fontSize: 15 }}>{animal.name}</div>
        <div style={{ fontSize: 12, color: '#8A8A99', marginTop: 2 }}>🔊 {animal.sound}</div>
      </div>
    </button>
  );
}
```

`src/presentation/components/AnimalDetail.tsx`:

```tsx
import type { CSSProperties } from 'react';
import type { Animal } from '../../domain/models/Animal';
import { letterCovers } from '../../styles/tokens';

interface AnimalDetailProps {
  animal: Animal;
  index: number;
  hasPrev: boolean;
  hasNext: boolean;
  onPrev: () => void;
  onNext: () => void;
  onClose: () => void;
}

export function AnimalDetail({ animal, index, hasPrev, hasNext, onPrev, onNext, onClose }: AnimalDetailProps) {
  const cover = letterCovers[index % letterCovers.length];
  return (
    <div
      onClick={onClose}
      style={{ position: 'fixed', inset: 0, background: 'rgba(20,10,40,.45)', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 20, zIndex: 50 }}
    >
      <div onClick={(e) => e.stopPropagation()} style={{ width: '100%', maxWidth: 420, background: '#fff', borderRadius: 24, overflow: 'hidden', animation: 'fadeUp .3s ease both' }}>
        <div style={{ background: cover, padding: 40, textAlign: 'center' }}>
          <div style={{ fontSize: 96, lineHeight: 1 }}>{animal.emoji}</div>
        </div>
        <div style={{ padding: '22px 26px 26px', textAlign: 'center' }}>
          <div style={{ fontFamily: 'Sora', fontWeight: 700, fontSize: 26, margin: '0 0 6px' }}>{animal.name}</div>
          <div style={{ fontSize: 17, color: '#4F46E5', fontWeight: 600, marginBottom: 18 }}>🔊 {animal.sound}</div>
          <div style={{ display: 'flex', gap: 10, justifyContent: 'space-between' }}>
            <button onClick={onPrev} disabled={!hasPrev} style={navBtnStyle(!hasPrev)}>← Prev</button>
            <button onClick={onClose} style={{ flex: 1, border: 'none', borderRadius: 12, background: 'linear-gradient(135deg,#4F46E5,#6D28D9)', color: '#fff', fontWeight: 700, fontFamily: 'Sora', cursor: 'pointer', padding: 12 }}>OK</button>
            <button onClick={onNext} disabled={!hasNext} style={navBtnStyle(!hasNext)}>Next →</button>
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

- [ ] **Step 2: Number components**

`src/presentation/components/NumberCard.tsx`:

```tsx
import type { CountingNumber } from '../../domain/models/CountingNumber';
import { letterCovers } from '../../styles/tokens';

interface NumberCardProps {
  number: CountingNumber;
  index: number;
  onClick: () => void;
}

export function NumberCard({ number, index, onClick }: NumberCardProps) {
  const cover = letterCovers[index % letterCovers.length];
  return (
    <button
      onClick={onClick}
      style={{ textAlign: 'left', border: '1px solid rgba(27,27,47,.06)', borderRadius: 18, overflow: 'hidden', cursor: 'pointer', background: '#fff', padding: 0 }}
    >
      <div style={{ height: 96, background: cover, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <span style={{ fontFamily: 'Sora', fontWeight: 800, fontSize: 48, color: 'rgba(255,255,255,.95)' }}>{number.value}</span>
      </div>
      <div style={{ padding: '14px 16px 16px' }}>
        <div style={{ fontFamily: 'Sora', fontWeight: 700, fontSize: 15 }}>{number.word}</div>
        <div style={{ fontSize: 14, marginTop: 4, letterSpacing: 2, overflowWrap: 'anywhere' }}>{number.emoji.repeat(number.value)}</div>
      </div>
    </button>
  );
}
```

`src/presentation/components/NumberDetail.tsx`:

```tsx
import type { CSSProperties } from 'react';
import type { CountingNumber } from '../../domain/models/CountingNumber';
import { letterCovers } from '../../styles/tokens';

interface NumberDetailProps {
  number: CountingNumber;
  index: number;
  hasPrev: boolean;
  hasNext: boolean;
  onPrev: () => void;
  onNext: () => void;
  onClose: () => void;
}

export function NumberDetail({ number, index, hasPrev, hasNext, onPrev, onNext, onClose }: NumberDetailProps) {
  const cover = letterCovers[index % letterCovers.length];
  return (
    <div
      onClick={onClose}
      style={{ position: 'fixed', inset: 0, background: 'rgba(20,10,40,.45)', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 20, zIndex: 50 }}
    >
      <div onClick={(e) => e.stopPropagation()} style={{ width: '100%', maxWidth: 420, background: '#fff', borderRadius: 24, overflow: 'hidden', animation: 'fadeUp .3s ease both' }}>
        <div style={{ background: cover, padding: 34, textAlign: 'center', color: '#fff' }}>
          <div style={{ fontFamily: 'Sora', fontWeight: 800, fontSize: 84, lineHeight: 1 }}>{number.value}</div>
        </div>
        <div style={{ padding: '22px 26px 26px', textAlign: 'center' }}>
          <div style={{ fontFamily: 'Sora', fontWeight: 700, fontSize: 26, margin: '0 0 10px' }}>{number.word}</div>
          <div style={{ fontSize: 28, letterSpacing: 4, marginBottom: 18, overflowWrap: 'anywhere' }}>{number.emoji.repeat(number.value)}</div>
          <div style={{ display: 'flex', gap: 10, justifyContent: 'space-between' }}>
            <button onClick={onPrev} disabled={!hasPrev} style={navBtnStyle(!hasPrev)}>← Prev</button>
            <button onClick={onClose} style={{ flex: 1, border: 'none', borderRadius: 12, background: 'linear-gradient(135deg,#4F46E5,#6D28D9)', color: '#fff', fontWeight: 700, fontFamily: 'Sora', cursor: 'pointer', padding: 12 }}>OK</button>
            <button onClick={onNext} disabled={!hasNext} style={navBtnStyle(!hasNext)}>Next →</button>
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

- [ ] **Step 3: Type-check + commit**

```bash
npx tsc -b
git add src/presentation/components/AnimalCard.tsx src/presentation/components/AnimalDetail.tsx src/presentation/components/NumberCard.tsx src/presentation/components/NumberDetail.tsx
git commit -m "feat: add animal and number card/detail components

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task B4: AnimalsPage + CountingPage + routes

**Files:**
- Create: `src/presentation/pages/AnimalsPage.tsx`, `src/presentation/pages/CountingPage.tsx`
- Modify: `src/presentation/router.tsx`

- [ ] **Step 1: Write `AnimalsPage`**

`src/presentation/pages/AnimalsPage.tsx`:

```tsx
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { container } from '../../di/container';
import type { Animal } from '../../domain/models/Animal';
import { AnimalCard } from '../components/AnimalCard';
import { AnimalDetail } from '../components/AnimalDetail';
import { LanguageSwitcher } from '../components/LanguageSwitcher';
import { useI18n } from '../i18n/useI18n';

type Status = 'loading' | 'ready' | 'error';

export function AnimalsPage() {
  const { lang, t } = useI18n();
  const navigate = useNavigate();
  const [animals, setAnimals] = useState<Animal[]>([]);
  const [status, setStatus] = useState<Status>('loading');
  const [selected, setSelected] = useState<number | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    setStatus('loading');
    container.getAnimals(lang)
      .then((data) => { if (active) { setAnimals(data); setStatus('ready'); } })
      .catch(() => { if (active) setStatus('error'); });
    return () => { active = false; };
  }, [lang, reloadKey]);

  return (
    <div style={{ minHeight: '100vh', background: '#F4F3EF' }}>
      <header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '22px 34px', position: 'sticky', top: 0, background: 'rgba(244,243,239,.88)', backdropFilter: 'blur(12px)', zIndex: 5 }}>
        <button onClick={() => navigate('/')} style={{ border: '1px solid rgba(27,27,47,.08)', background: '#fff', borderRadius: 12, padding: '10px 16px', cursor: 'pointer', fontWeight: 600, fontSize: 13, color: '#5A5A6B' }}>
          ← {t('app.title')}
        </button>
        <LanguageSwitcher />
      </header>

      <main style={{ padding: '6px 34px 60px' }}>
        <h1 style={{ fontFamily: 'Sora', fontWeight: 800, fontSize: 26, letterSpacing: '-.02em', margin: '0 0 22px' }}>
          {t('app.chooseLesson')}
        </h1>

        {status === 'loading' && <p style={{ color: '#6B6B80' }}>{t('common.loading')}</p>}

        {status === 'error' && (
          <div style={{ background: '#FEF2F2', border: '1px solid #FECACA', color: '#B91C1C', padding: '12px 14px', borderRadius: 12, display: 'inline-flex', gap: 12, alignItems: 'center' }}>
            {t('common.error')}
            <button onClick={() => setReloadKey((k) => k + 1)} style={{ border: 'none', background: '#B91C1C', color: '#fff', borderRadius: 8, padding: '6px 12px', cursor: 'pointer' }}>{t('common.retry')}</button>
          </div>
        )}

        {status === 'ready' && (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(150px,1fr))', gap: 16 }}>
            {animals.map((animal, index) => (
              <AnimalCard key={animal.code} animal={animal} index={index} onClick={() => setSelected(index)} />
            ))}
          </div>
        )}
      </main>

      {selected !== null && animals[selected] && (
        <AnimalDetail
          animal={animals[selected]}
          index={selected}
          hasPrev={selected > 0}
          hasNext={selected < animals.length - 1}
          onPrev={() => setSelected((s) => (s === null ? s : Math.max(0, s - 1)))}
          onNext={() => setSelected((s) => (s === null ? s : Math.min(animals.length - 1, s + 1)))}
          onClose={() => setSelected(null)}
        />
      )}
    </div>
  );
}
```

- [ ] **Step 2: Write `CountingPage`**

`src/presentation/pages/CountingPage.tsx`:

```tsx
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { container } from '../../di/container';
import type { CountingNumber } from '../../domain/models/CountingNumber';
import { LanguageSwitcher } from '../components/LanguageSwitcher';
import { NumberCard } from '../components/NumberCard';
import { NumberDetail } from '../components/NumberDetail';
import { useI18n } from '../i18n/useI18n';

type Status = 'loading' | 'ready' | 'error';

export function CountingPage() {
  const { lang, t } = useI18n();
  const navigate = useNavigate();
  const [numbers, setNumbers] = useState<CountingNumber[]>([]);
  const [status, setStatus] = useState<Status>('loading');
  const [selected, setSelected] = useState<number | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    setStatus('loading');
    container.getCounting(lang)
      .then((data) => { if (active) { setNumbers(data); setStatus('ready'); } })
      .catch(() => { if (active) setStatus('error'); });
    return () => { active = false; };
  }, [lang, reloadKey]);

  return (
    <div style={{ minHeight: '100vh', background: '#F4F3EF' }}>
      <header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '22px 34px', position: 'sticky', top: 0, background: 'rgba(244,243,239,.88)', backdropFilter: 'blur(12px)', zIndex: 5 }}>
        <button onClick={() => navigate('/')} style={{ border: '1px solid rgba(27,27,47,.08)', background: '#fff', borderRadius: 12, padding: '10px 16px', cursor: 'pointer', fontWeight: 600, fontSize: 13, color: '#5A5A6B' }}>
          ← {t('app.title')}
        </button>
        <LanguageSwitcher />
      </header>

      <main style={{ padding: '6px 34px 60px' }}>
        <h1 style={{ fontFamily: 'Sora', fontWeight: 800, fontSize: 26, letterSpacing: '-.02em', margin: '0 0 22px' }}>
          {t('app.chooseLesson')}
        </h1>

        {status === 'loading' && <p style={{ color: '#6B6B80' }}>{t('common.loading')}</p>}

        {status === 'error' && (
          <div style={{ background: '#FEF2F2', border: '1px solid #FECACA', color: '#B91C1C', padding: '12px 14px', borderRadius: 12, display: 'inline-flex', gap: 12, alignItems: 'center' }}>
            {t('common.error')}
            <button onClick={() => setReloadKey((k) => k + 1)} style={{ border: 'none', background: '#B91C1C', color: '#fff', borderRadius: 8, padding: '6px 12px', cursor: 'pointer' }}>{t('common.retry')}</button>
          </div>
        )}

        {status === 'ready' && (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(150px,1fr))', gap: 16 }}>
            {numbers.map((number, index) => (
              <NumberCard key={number.value} number={number} index={index} onClick={() => setSelected(index)} />
            ))}
          </div>
        )}
      </main>

      {selected !== null && numbers[selected] && (
        <NumberDetail
          number={numbers[selected]}
          index={selected}
          hasPrev={selected > 0}
          hasNext={selected < numbers.length - 1}
          onPrev={() => setSelected((s) => (s === null ? s : Math.max(0, s - 1)))}
          onNext={() => setSelected((s) => (s === null ? s : Math.min(numbers.length - 1, s + 1)))}
          onClose={() => setSelected(null)}
        />
      )}
    </div>
  );
}
```

- [ ] **Step 3: Add routes**

In `src/presentation/router.tsx`, add imports `AnimalsPage`, `CountingPage` and two guarded routes after `/colors`:

```tsx
  {
    path: '/animals',
    element: (
      <RequireAuth>
        <AnimalsPage />
      </RequireAuth>
    ),
  },
  {
    path: '/counting',
    element: (
      <RequireAuth>
        <CountingPage />
      </RequireAuth>
    ),
  },
```

(No HomePage change — `openProduct` already navigates `/<code>` for available products.)

- [ ] **Step 4: Build + tests + commit**

```bash
npm run build && npm test
git add src/presentation/pages/AnimalsPage.tsx src/presentation/pages/CountingPage.tsx src/presentation/router.tsx
git commit -m "feat: add animals and counting learning pages with routes

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

# Phase C — Deploy & verify

## Task C1: Rebuild the stack and verify both lessons end-to-end

- [ ] **Step 1: Rebuild**

```bash
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/orchestrator/deploy
docker compose up --build -d
for i in $(seq 1 60); do curl -s -o /dev/null "http://localhost:8080/api/categories" && break; sleep 2; done
```

- [ ] **Step 2: Verify through nginx**

```bash
TOKEN=$(curl -s -X POST "http://localhost:8080/api/users/login" -H 'Content-Type: application/json' -d '{"identifier":"demo@dlearning.vn","password":"Demo@123"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
curl -s -o /dev/null -w "animals no-token: %{http_code}\n" http://localhost:8080/api/animals             # 401
echo "animals count: $(curl -s 'http://localhost:8080/api/animals?lang=vi' -H "Authorization: Bearer $TOKEN" | grep -o '"code"' | wc -l | tr -d ' ')"     # 12
echo "counting count: $(curl -s 'http://localhost:8080/api/counting?lang=en' -H "Authorization: Bearer $TOKEN" | grep -o '"value"' | wc -l | tr -d ' ')"  # 10
echo "available products: $(curl -s 'http://localhost:8080/api/categories/preschool/products?lang=vi' | grep -o '"isAvailable":true' | wc -l | tr -d ' ')" # 4
```

- [ ] **Step 3: Browser check**

Open `http://localhost:8080`, log in — all four preschool cards live; Animals shows 12 cards with sounds, Counting shows 1–10 with repeated emojis; VI/EN switcher localizes everything. Leave the stack running for the user.

---

# Self-Review (coverage map)

- Spec §3.1 Animals slice → A1, A2, A3 (config/seed), A4 (resx), A5 (endpoint). §3.2 Counting slice → same tasks.
- Spec §3.3 migration + both product flips → A3. §3.4 API contract → A5. §3.5 tests (unit, integration, catalog 2→4, arch green) → A2, A6.
- Spec §4.1/4.2 FE stacks → B1, B2, B3, B4. §4.3 router (no HomePage change) → B4. §4.4 FE tests → B1.
- Spec §5 deploy/verify at :8080 → C1.
