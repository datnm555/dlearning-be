# dLearning — Catalog + i18n + Docker Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Category/Product taxonomy served in English & Vietnamese, a frontend language switcher + menu that loads from the API, and a one-command full-stack Docker deployment.

**Architecture:** A new `Catalog` vertical slice in the .NET backend stores only structural data (codes/icons/order); display names come from `.resx` resource files resolved in the Web.Api endpoints via `IStringLocalizer` + ASP.NET Core request localization. The React frontend gets a JSON-catalog i18n layer, a home menu page that fetches categories/products, and both apps get Dockerfiles wired together by a compose file in the orchestrator.

**Tech Stack:** .NET 10, EF Core 10, `Microsoft.Extensions.Localization` (shared framework), Vite + React 19 + TypeScript, Vitest, nginx, Docker Compose.

## Global Constraints

- **Backend repo:** `/Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be` (branch `main`). **Frontend repo:** `/Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-web` (branch `main`). **Orchestrator:** `/Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/orchestrator`. Commit in the repo the changed files belong to.
- **Central Package Management** — no `Version=` on `<PackageReference>`; versions in `Directory.Packages.props`. (No new NuGet packages are needed — `Microsoft.Extensions.Localization` ships with the ASP.NET Core shared framework used by Web.Api.)
- **`TreatWarningsAsErrors` + `AnalysisMode=All` + `EnforceCodeStyleInBuild`** — analyzer/style violations fail the build. File-scoped namespaces, braces always, `I`-prefixed interfaces, 4-space indent for `.cs` / 2-space for `.json`/`.csproj`.
- **Dependency rule** `SharedKernel → Domain → Application → Infrastructure → Web.Api`, enforced by `tests/ArchitectureTests`. **Application must NOT reference `Microsoft.AspNetCore` or localization** — localization lives only in Web.Api. Handlers are `internal sealed` and implement `IQueryHandler<,>`.
- **Feature folders plural, aggregate singular** (`Catalog/`, `Category`, `Product`). Result pattern; endpoints end with `result.ToHttpResult(...)`.
- **EF migrations** live in `src/Infrastructure/Database/Migrations/` (use `-o Database/Migrations`); run `dotnet ef` with `ASPNETCORE_ENVIRONMENT=Production` so the dev auto-migrate is skipped at design time. Migration files are analyzer-exempt via `.editorconfig`.
- **Supported cultures `en`, `vi`; default `vi`.** Culture chosen from `?lang=` first, then `Accept-Language`.
- **Category codes:** `preschool`, `primary`, `secondary`, `highschool`, `university`. **Product codes (preschool):** `alphabet` (IsAvailable=true), `animals`, `colors`, `counting` (IsAvailable=false).
- **Static seed GUIDs:** categories `ca7e0000-0000-4000-8000-0000000000NN` (NN = order 01..05); products `9d0d0000-0000-4000-8000-0000000000NN` (NN = order 01..04).
- **Frontend:** `erasableSyntaxOnly` is ON — no constructor parameter properties; declare fields and assign in the body. Dependency direction `presentation → application → domain`, `infrastructure → domain`, `di` composes. `localStorage` language key `dlearning.lang`, default `vi`.
- **API contract (both dev proxy and nginx use `/api` → backend root):** `GET /categories?lang=`, `GET /categories/{code}/products?lang=` are **anonymous**; `/users/*` and `/alphabet` unchanged.

---

# Phase A — Backend Catalog slice (`dlearning-be`)

## Task A1: `Category` and `Product` domain entities

**Files:**
- Create: `src/Domain/Catalog/Category.cs`
- Create: `src/Domain/Catalog/Product.cs`
- Test: `tests/Application.UnitTests/Catalog/CategoryTests.cs`

**Interfaces:**
- Produces: `Category : AggregateRoot` with `init` props `Code`, `IconKey`, `DisplayOrder`; `Product : Entity` with `init` props `CategoryId (Guid)`, `Code`, `IconKey`, `DisplayOrder`, `IsAvailable (bool)`. Both use object-initializer construction (like `AlphabetLetter`); no factory/validation (reference data seeded via EF).

- [ ] **Step 1: Write the failing test**

Create `tests/Application.UnitTests/Catalog/CategoryTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~CategoryTests"`
Expected: FAIL (build error — `Domain.Catalog.Category` does not exist).

- [ ] **Step 3: Write the entities**

Create `src/Domain/Catalog/Category.cs`:

```csharp
using SharedKernel;

namespace Domain.Catalog;

public sealed class Category : AggregateRoot
{
    public string Code { get; init; } = string.Empty;

    public string IconKey { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }
}
```

Create `src/Domain/Catalog/Product.cs`:

```csharp
using SharedKernel;

namespace Domain.Catalog;

public sealed class Product : Entity
{
    public Guid CategoryId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string IconKey { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public bool IsAvailable { get; init; }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~CategoryTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be
git add src/Domain/Catalog tests/Application.UnitTests/Catalog/CategoryTests.cs
git commit -m "feat: add Category and Product domain entities

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A2: Catalog queries + handlers + DbSets

**Files:**
- Create: `src/Application/Catalog/Data/CategoryDto.cs`
- Create: `src/Application/Catalog/Data/ProductDto.cs`
- Create: `src/Application/Catalog/GetCategoriesQuery.cs`
- Create: `src/Application/Catalog/GetCategoriesQueryHandler.cs`
- Create: `src/Application/Catalog/GetProductsByCategoryQuery.cs`
- Create: `src/Application/Catalog/GetProductsByCategoryQueryHandler.cs`
- Modify: `src/Application/Abstractions/Data/IApplicationDbContext.cs`
- Modify: `src/Infrastructure/Database/ApplicationDbContext.cs`
- Modify: `src/Application/DependencyInjection.cs`
- Test: `tests/Application.UnitTests/Catalog/GetCategoriesQueryHandlerTests.cs`
- Test: `tests/Application.UnitTests/Catalog/GetProductsByCategoryQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `IApplicationDbContext` (add `Categories`, `Products` DbSets), `Category`, `Product`.
- Produces: `CategoryDto(string Code, string IconKey, int DisplayOrder)`; `ProductDto(string Code, string IconKey, int DisplayOrder, bool IsAvailable)`; `GetCategoriesQuery() : IQuery<IReadOnlyList<CategoryDto>>`; `GetProductsByCategoryQuery(string CategoryCode) : IQuery<IReadOnlyList<ProductDto>>`. Unknown category code → empty list (success).

- [ ] **Step 1: Write the failing tests**

Create `tests/Application.UnitTests/Catalog/GetCategoriesQueryHandlerTests.cs`:

```csharp
using Application.Abstractions.Data;
using Application.Catalog;
using Domain.Catalog;
using MockQueryable.NSubstitute;
using NSubstitute;
using Shouldly;

namespace Application.UnitTests.Catalog;

public class GetCategoriesQueryHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();

    [Fact]
    public async Task Handle_ReturnsCategoriesOrderedByDisplayOrder()
    {
        var categories = new List<Category>
        {
            new() { Code = "primary", IconKey = "✏️", DisplayOrder = 2 },
            new() { Code = "preschool", IconKey = "🧸", DisplayOrder = 1 }
        };
        var set = categories.BuildMockDbSet();
        _dbContext.Categories.Returns(set);
        var handler = new GetCategoriesQueryHandler(_dbContext);

        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].Code.ShouldBe("preschool");
        result.Value[1].Code.ShouldBe("primary");
    }
}
```

Create `tests/Application.UnitTests/Catalog/GetProductsByCategoryQueryHandlerTests.cs`.

Note on the seed ids: `Entity.Id` has a `protected internal` setter, so this test project cannot set `Id` in an object initializer — the preschool `Category` therefore keeps its default `Id` (`Guid.Empty`), and the preschool products reference it with `CategoryId = Guid.Empty`. The "other" product points at a different category id so it is correctly excluded. The handler (Step 3) matches products to a category by that category's `Code`, which is what makes this work.

```csharp
using Application.Abstractions.Data;
using Application.Catalog;
using Domain.Catalog;
using MockQueryable.NSubstitute;
using NSubstitute;
using Shouldly;

namespace Application.UnitTests.Catalog;

public class GetProductsByCategoryQueryHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();

    private GetProductsByCategoryQueryHandler CreateHandler()
    {
        var categories = new List<Category>
        {
            new() { Code = "preschool", IconKey = "🧸", DisplayOrder = 1 } // Id defaults to Guid.Empty
        };
        var products = new List<Product>
        {
            new() { CategoryId = Guid.Empty, Code = "animals", IconKey = "🐾", DisplayOrder = 2, IsAvailable = false },
            new() { CategoryId = Guid.Empty, Code = "alphabet", IconKey = "🔤", DisplayOrder = 1, IsAvailable = true },
            new() { CategoryId = new Guid("ca7e0000-0000-4000-8000-0000000000ff"), Code = "other", IconKey = "x", DisplayOrder = 1, IsAvailable = false }
        };
        var catSet = categories.BuildMockDbSet();
        var prodSet = products.BuildMockDbSet();
        _dbContext.Categories.Returns(catSet);
        _dbContext.Products.Returns(prodSet);
        return new GetProductsByCategoryQueryHandler(_dbContext);
    }

    [Fact]
    public async Task Handle_ReturnsProductsForCategory_OrderedAndFiltered()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new GetProductsByCategoryQuery("preschool"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].Code.ShouldBe("alphabet");
        result.Value[0].IsAvailable.ShouldBeTrue();
        result.Value[1].Code.ShouldBe("animals");
    }

    [Fact]
    public async Task Handle_UnknownCategory_ReturnsEmpty()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new GetProductsByCategoryQuery("nope"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~Catalog"`
Expected: FAIL (build error — query types don't exist).

- [ ] **Step 3: Write DTOs, queries, handlers**

Create `src/Application/Catalog/Data/CategoryDto.cs`:

```csharp
namespace Application.Catalog.Data;

public sealed record CategoryDto(string Code, string IconKey, int DisplayOrder);
```

Create `src/Application/Catalog/Data/ProductDto.cs`:

```csharp
namespace Application.Catalog.Data;

public sealed record ProductDto(string Code, string IconKey, int DisplayOrder, bool IsAvailable);
```

Create `src/Application/Catalog/GetCategoriesQuery.cs`:

```csharp
using Application.Abstractions.Messaging;
using Application.Catalog.Data;

namespace Application.Catalog;

public sealed record GetCategoriesQuery : IQuery<IReadOnlyList<CategoryDto>>;
```

Create `src/Application/Catalog/GetCategoriesQueryHandler.cs`:

```csharp
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Catalog.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Catalog;

internal sealed class GetCategoriesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(
        GetCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        List<CategoryDto> categories = await dbContext.Categories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto(c.Code, c.IconKey, c.DisplayOrder))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CategoryDto>>(categories);
    }
}
```

Create `src/Application/Catalog/GetProductsByCategoryQuery.cs`:

```csharp
using Application.Abstractions.Messaging;
using Application.Catalog.Data;

namespace Application.Catalog;

public sealed record GetProductsByCategoryQuery(string CategoryCode) : IQuery<IReadOnlyList<ProductDto>>;
```

Create `src/Application/Catalog/GetProductsByCategoryQueryHandler.cs`:

```csharp
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Catalog.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Catalog;

internal sealed class GetProductsByCategoryQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetProductsByCategoryQuery, IReadOnlyList<ProductDto>>
{
    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(
        GetProductsByCategoryQuery query,
        CancellationToken cancellationToken)
    {
        string code = (query.CategoryCode ?? string.Empty).Trim().ToLowerInvariant();

        // Correlated EXISTS: keep products whose category has the requested code.
        // Translates to SQL EXISTS in real EF; runs as an in-memory Any() under MockQueryable.
        List<ProductDto> products = await dbContext.Products
            .Where(p => dbContext.Categories.Any(c => c.Id == p.CategoryId && c.Code == code))
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new ProductDto(p.Code, p.IconKey, p.DisplayOrder, p.IsAvailable))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ProductDto>>(products);
    }
}
```

- [ ] **Step 4: Expose DbSets and register handlers**

In `src/Application/Abstractions/Data/IApplicationDbContext.cs`, add the two DbSets (and `using Domain.Catalog;`) alongside the existing ones:

```csharp
using Domain.Alphabets;
using Domain.Catalog;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<AlphabetLetter> AlphabetLetters { get; }

    DbSet<Category> Categories { get; }

    DbSet<Product> Products { get; }

    DbSet<T> Set<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

In `src/Infrastructure/Database/ApplicationDbContext.cs`, add the DbSet properties (and `using Domain.Catalog;`):

```csharp
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();
```

In `src/Application/DependencyInjection.cs`, register both handlers (add usings `Application.Catalog;`, `Application.Catalog.Data;`):

```csharp
        services.AddScoped<IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>, GetCategoriesQueryHandler>();
        services.AddScoped<IQueryHandler<GetProductsByCategoryQuery, IReadOnlyList<ProductDto>>, GetProductsByCategoryQueryHandler>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~Catalog"`
Expected: PASS (CategoryTests + both handler test classes green).

- [ ] **Step 6: Commit**

```bash
git add src/Application/Catalog src/Application/Abstractions/Data/IApplicationDbContext.cs src/Infrastructure/Database/ApplicationDbContext.cs src/Application/DependencyInjection.cs tests/Application.UnitTests/Catalog
git commit -m "feat: add GetCategories and GetProductsByCategory queries

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A3: EF configuration + seed + migration

**Files:**
- Create: `src/Infrastructure/Catalog/CategoryConfiguration.cs`
- Create: `src/Infrastructure/Catalog/ProductConfiguration.cs`
- Create: `src/Infrastructure/Database/Migrations/*_Catalog.cs` (generated)

**Interfaces:**
- Consumes: `Category`, `Product`.
- Produces: `categories` + `products` tables with unique indexes and the demo seed (5 categories, 4 preschool products).

- [ ] **Step 1: Write the category configuration**

Create `src/Infrastructure/Catalog/CategoryConfiguration.cs`:

```csharp
using System.Globalization;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Catalog;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).HasMaxLength(32).IsRequired();
        builder.Property(c => c.IconKey).HasMaxLength(16).IsRequired();
        builder.Property(c => c.DisplayOrder).IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();
        builder.Ignore(c => c.DomainEvents);

        builder.HasData(Seed());
    }

    private static object[] Seed()
    {
        (string Code, string Icon)[] rows =
        [
            ("preschool", "🧸"),
            ("primary", "✏️"),
            ("secondary", "📐"),
            ("highschool", "🎓"),
            ("university", "🏛️")
        ];

        var seed = new object[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            int order = i + 1;
            string hex = order.ToString("x2", CultureInfo.InvariantCulture);
            seed[i] = new
            {
                Id = new Guid("ca7e0000-0000-4000-8000-0000000000" + hex),
                Code = rows[i].Code,
                IconKey = rows[i].Icon,
                DisplayOrder = order
            };
        }

        return seed;
    }
}
```

- [ ] **Step 2: Write the product configuration**

Create `src/Infrastructure/Catalog/ProductConfiguration.cs`:

```csharp
using System.Globalization;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Catalog;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    // Products all belong to the preschool category (seed GUID order 01).
    private static readonly Guid PreschoolId = new("ca7e0000-0000-4000-8000-000000000001");

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.CategoryId).IsRequired();
        builder.Property(p => p.Code).HasMaxLength(32).IsRequired();
        builder.Property(p => p.IconKey).HasMaxLength(16).IsRequired();
        builder.Property(p => p.DisplayOrder).IsRequired();
        builder.Property(p => p.IsAvailable).IsRequired();

        builder.HasIndex(p => new { p.CategoryId, p.Code }).IsUnique();
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(p => p.DomainEvents);

        builder.HasData(Seed());
    }

    private static object[] Seed()
    {
        (string Code, string Icon, bool Available)[] rows =
        [
            ("alphabet", "🔤", true),
            ("animals", "🐾", false),
            ("colors", "🎨", false),
            ("counting", "🔢", false)
        ];

        var seed = new object[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            int order = i + 1;
            string hex = order.ToString("x2", CultureInfo.InvariantCulture);
            seed[i] = new
            {
                Id = new Guid("9d0d0000-0000-4000-8000-0000000000" + hex),
                CategoryId = PreschoolId,
                Code = rows[i].Code,
                IconKey = rows[i].Icon,
                DisplayOrder = order,
                IsAvailable = rows[i].Available
            };
        }

        return seed;
    }
}
```

- [ ] **Step 3: Build**

Run: `cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be && dotnet build DLearning.slnx`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Ensure DB is up, create + apply the migration**

Run:
```bash
docker compose up -d
dotnet tool restore
ASPNETCORE_ENVIRONMENT=Production dotnet ef migrations add Catalog --project src/Infrastructure --startup-project src/Web.Api -o Database/Migrations
ASPNETCORE_ENVIRONMENT=Production dotnet ef database update --project src/Infrastructure --startup-project src/Web.Api
```
Expected: `Done.` then `Applying migration '..._Catalog'. Done.` New files under `src/Infrastructure/Database/Migrations/` create `categories` + `products` and insert 5 + 4 rows.

- [ ] **Step 5: Verify the seed**

Run:
```bash
docker exec dlearning-postgres psql -U postgres -d dlearning -t -c 'select "Code" from categories order by "DisplayOrder";'
docker exec dlearning-postgres psql -U postgres -d dlearning -t -c 'select "Code", "IsAvailable" from products order by "DisplayOrder";'
```
Expected: 5 category codes (`preschool`…`university`); 4 products with `alphabet` = `t`, others `f`.

- [ ] **Step 6: Commit**

```bash
git add src/Infrastructure/Catalog src/Infrastructure/Database/Migrations
git commit -m "feat: add EF config + migration seeding categories and preschool products

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A4: Localization resources + request localization

**Files:**
- Create: `src/Web.Api/Resources/SharedResource.cs`
- Create: `src/Web.Api/Resources/SharedResource.en.resx`
- Create: `src/Web.Api/Resources/SharedResource.vi.resx`
- Modify: `src/Web.Api/Program.cs`

**Interfaces:**
- Produces: an `IStringLocalizer<SharedResource>` resolving keys `Category.<code>` / `Product.<code>`; request-localization middleware honoring `?lang=` then `Accept-Language`, cultures `en`/`vi`, default `vi`.

- [ ] **Step 1: Write the resource marker class**

Create `src/Web.Api/Resources/SharedResource.cs`:

```csharp
namespace Web.Api.Resources;

/// <summary>
/// Marker type for the shared .resx resources (SharedResource.en.resx / SharedResource.vi.resx).
/// Used as the generic argument of IStringLocalizer&lt;SharedResource&gt;.
/// </summary>
public sealed class SharedResource;
```

- [ ] **Step 2: Write the English resources**

Create `src/Web.Api/Resources/SharedResource.en.resx` (standard .resx XML; only the `<data>` entries matter):

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <data name="Category.preschool" xml:space="preserve"><value>Preschool</value></data>
  <data name="Category.primary" xml:space="preserve"><value>Primary School</value></data>
  <data name="Category.secondary" xml:space="preserve"><value>Secondary School</value></data>
  <data name="Category.highschool" xml:space="preserve"><value>High School</value></data>
  <data name="Category.university" xml:space="preserve"><value>University</value></data>
  <data name="Product.alphabet" xml:space="preserve"><value>Alphabet</value></data>
  <data name="Product.animals" xml:space="preserve"><value>Animals</value></data>
  <data name="Product.colors" xml:space="preserve"><value>Colors</value></data>
  <data name="Product.counting" xml:space="preserve"><value>Counting 1–10</value></data>
</root>
```

- [ ] **Step 3: Write the Vietnamese resources**

Create `src/Web.Api/Resources/SharedResource.vi.resx` (same structure, Vietnamese values):

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <data name="Category.preschool" xml:space="preserve"><value>Mầm non</value></data>
  <data name="Category.primary" xml:space="preserve"><value>Cấp 1</value></data>
  <data name="Category.secondary" xml:space="preserve"><value>Cấp 2</value></data>
  <data name="Category.highschool" xml:space="preserve"><value>Cấp 3</value></data>
  <data name="Category.university" xml:space="preserve"><value>Đại học</value></data>
  <data name="Product.alphabet" xml:space="preserve"><value>Bảng chữ cái</value></data>
  <data name="Product.animals" xml:space="preserve"><value>Con vật</value></data>
  <data name="Product.colors" xml:space="preserve"><value>Màu sắc</value></data>
  <data name="Product.counting" xml:space="preserve"><value>Học đếm 1–10</value></data>
</root>
```

- [ ] **Step 4: Wire localization in `Program.cs`**

In `src/Web.Api/Program.cs`, add localization services after `AddProblemDetails()`:

```csharp
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
```

Add the request-localization options just before `WebApplication app = builder.Build();`:

```csharp
var supportedCultures = new[] { "en", "vi" };
builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("vi")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
    options.RequestCultureProviders =
    [
        new Microsoft.AspNetCore.Localization.QueryStringRequestCultureProvider { QueryStringKey = "lang", UIQueryStringKey = "lang" },
        new Microsoft.AspNetCore.Localization.AcceptLanguageHeaderRequestCultureProvider()
    ];
});
```

Add the middleware right after `app.UseExceptionHandler();` (before `UseCors`/`UseAuthentication`):

```csharp
app.UseRequestLocalization();
```

- [ ] **Step 5: Build**

Run: `dotnet build DLearning.slnx`
Expected: `Build succeeded. 0 Error(s)`. (The `.resx` files compile into satellite resources automatically for an SDK project.)

- [ ] **Step 6: Commit**

```bash
git add src/Web.Api/Resources src/Web.Api/Program.cs
git commit -m "feat: add en/vi resx resources and request localization

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A5: Catalog endpoints (localized, anonymous)

**Files:**
- Create: `src/Web.Api/Endpoints/Catalog/GetCategories.cs`
- Create: `src/Web.Api/Endpoints/Catalog/GetProductsByCategory.cs`

**Interfaces:**
- Consumes: `IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>`, `IQueryHandler<GetProductsByCategoryQuery, IReadOnlyList<ProductDto>>`, `IStringLocalizer<SharedResource>`.
- Produces: `GET /categories` → `[{ code, name, iconKey, displayOrder }]`; `GET /categories/{code}/products` → `[{ code, name, iconKey, displayOrder, isAvailable }]`.

- [ ] **Step 1: Write the categories endpoint**

Create `src/Web.Api/Endpoints/Catalog/GetCategories.cs`:

```csharp
using Application.Abstractions.Messaging;
using Application.Catalog;
using Application.Catalog.Data;
using Microsoft.Extensions.Localization;
using SharedKernel;
using Web.Api.Infrastructure;
using Web.Api.Middleware;
using Web.Api.Resources;

namespace Web.Api.Endpoints.Catalog;

internal sealed class GetCategories : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/categories", async (
            IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>> handler,
            IStringLocalizer<SharedResource> localizer,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<CategoryDto>> result =
                await handler.Handle(new GetCategoriesQuery(), cancellationToken);

            return result.ToHttpResult(categories => Results.Ok(
                categories.Select(c => new
                {
                    c.Code,
                    name = localizer["Category." + c.Code].Value,
                    c.IconKey,
                    c.DisplayOrder
                })));
        });
    }
}
```

- [ ] **Step 2: Write the products endpoint**

Create `src/Web.Api/Endpoints/Catalog/GetProductsByCategory.cs`:

```csharp
using Application.Abstractions.Messaging;
using Application.Catalog;
using Application.Catalog.Data;
using Microsoft.Extensions.Localization;
using SharedKernel;
using Web.Api.Infrastructure;
using Web.Api.Middleware;
using Web.Api.Resources;

namespace Web.Api.Endpoints.Catalog;

internal sealed class GetProductsByCategory : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/categories/{code}/products", async (
            string code,
            IQueryHandler<GetProductsByCategoryQuery, IReadOnlyList<ProductDto>> handler,
            IStringLocalizer<SharedResource> localizer,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<ProductDto>> result =
                await handler.Handle(new GetProductsByCategoryQuery(code), cancellationToken);

            return result.ToHttpResult(products => Results.Ok(
                products.Select(p => new
                {
                    p.Code,
                    name = localizer["Product." + p.Code].Value,
                    p.IconKey,
                    p.DisplayOrder,
                    p.IsAvailable
                })));
        });
    }
}
```

- [ ] **Step 3: Build + manual smoke test**

Run: `dotnet build DLearning.slnx` → `Build succeeded.`

Run the API and check localization (DB already migrated in A3):
```bash
docker compose up -d
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Web.Api &
sleep 8
curl -s "http://localhost:5113/categories?lang=en" | head -c 200; echo
curl -s "http://localhost:5113/categories?lang=vi" | head -c 200; echo
curl -s "http://localhost:5113/categories/preschool/products?lang=vi" | grep -o '"code":"[a-z]*"' | wc -l
pkill -f Web.Api
```
Expected: `?lang=en` shows `"name":"Preschool"`; `?lang=vi` shows `"name":"Mầm non"`; the products call returns `4`.

- [ ] **Step 4: Commit**

```bash
git add src/Web.Api/Endpoints/Catalog
git commit -m "feat: add localized anonymous catalog endpoints

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A6: Catalog integration tests

**Files:**
- Create: `tests/Api.IntegrationTests/Catalog/CatalogEndpointsTests.cs`

**Interfaces:**
- Consumes: `ApiTestFactory` (Testcontainers Postgres; seed comes from the migration).

- [ ] **Step 1: Write the integration tests**

Create `tests/Api.IntegrationTests/Catalog/CatalogEndpointsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Api.IntegrationTests.Catalog;

public sealed class CatalogEndpointsTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record CategoryView(string Code, string Name, string IconKey, int DisplayOrder);
    private sealed record ProductView(string Code, string Name, string IconKey, int DisplayOrder, bool IsAvailable);

    [Fact]
    public async Task GetCategories_En_ReturnsEnglishNames()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryView>>("/categories?lang=en");

        categories.ShouldNotBeNull();
        categories.Count.ShouldBe(5);
        categories[0].Code.ShouldBe("preschool");
        categories[0].Name.ShouldBe("Preschool");
    }

    [Fact]
    public async Task GetCategories_Vi_ReturnsVietnameseNames()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryView>>("/categories?lang=vi");

        categories.ShouldNotBeNull();
        categories[0].Name.ShouldBe("Mầm non");
    }

    [Fact]
    public async Task GetCategories_NoLang_DefaultsToVietnamese()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryView>>("/categories");

        categories.ShouldNotBeNull();
        categories[0].Name.ShouldBe("Mầm non");
    }

    [Fact]
    public async Task GetProducts_Preschool_ReturnsFour_WithAlphabetAvailable()
    {
        var products = await _client.GetFromJsonAsync<List<ProductView>>("/categories/preschool/products?lang=en");

        products.ShouldNotBeNull();
        products.Count.ShouldBe(4);
        products[0].Code.ShouldBe("alphabet");
        products[0].Name.ShouldBe("Alphabet");
        products[0].IsAvailable.ShouldBeTrue();
        products.Count(p => p.IsAvailable).ShouldBe(1);
    }

    [Fact]
    public async Task GetProducts_EmptyCategory_ReturnsEmpty()
    {
        var response = await _client.GetAsync("/categories/university/products");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<ProductView>>();
        products.ShouldNotBeNull();
        products.ShouldBeEmpty();
    }
}
```

- [ ] **Step 2: Run the full backend suite**

Run: `cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be && dotnet test`
Expected: PASS — Architecture + Unit + Integration all green (integration needs Docker).

- [ ] **Step 3: Commit**

```bash
git add tests/Api.IntegrationTests/Catalog
git commit -m "test: add integration tests for localized catalog endpoints

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task A7: Backend Dockerfile

**Files:**
- Create: `dlearning-be/Dockerfile`
- Create: `dlearning-be/.dockerignore`

- [ ] **Step 1: Write `.dockerignore`**

Create `dlearning-be/.dockerignore`:

```
**/bin/
**/obj/
.git/
.vs/
.idea/
docs/
```

- [ ] **Step 2: Write the Dockerfile**

Create `dlearning-be/Dockerfile`:

```dockerfile
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore DLearning.slnx
RUN dotnet publish src/Web.Api/Web.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Web.Api.dll"]
```

- [ ] **Step 3: Build the image**

Run: `cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be && docker build -t dlearning-be:local .`
Expected: image builds; final line `naming to docker.io/library/dlearning-be:local`.

- [ ] **Step 4: Commit**

```bash
git add Dockerfile .dockerignore
git commit -m "chore: add backend Dockerfile

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

# Phase B — Frontend menu + i18n (`dlearning-web`)

> Paths relative to `/Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-web`.

## Task B1: i18n catalogs, LanguageProvider, useI18n

**Files:**
- Create: `src/i18n/en.json`, `src/i18n/vi.json`
- Create: `src/presentation/i18n/LanguageContext.ts`
- Create: `src/presentation/i18n/LanguageProvider.tsx`
- Create: `src/presentation/i18n/useI18n.ts`
- Create: `src/presentation/i18n/useI18n.test.tsx`

**Interfaces:**
- Produces: `useI18n()` → `{ lang: 'en'|'vi', setLang(l), t(key: string): string }`; language persisted in `localStorage['dlearning.lang']`, default `vi`.

- [ ] **Step 1: Write the failing test**

Create `src/presentation/i18n/useI18n.test.tsx`:

```tsx
import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import { LanguageProvider } from './LanguageProvider';
import { useI18n } from './useI18n';

function Probe() {
  const { lang, setLang, t } = useI18n();
  return (
    <div>
      <span data-testid="lang">{lang}</span>
      <span data-testid="title">{t('app.chooseLesson')}</span>
      <button onClick={() => setLang('en')}>en</button>
    </div>
  );
}

describe('useI18n', () => {
  beforeEach(() => localStorage.clear());

  it('defaults to vi and translates, switches + persists on setLang', () => {
    render(<LanguageProvider><Probe /></LanguageProvider>);
    expect(screen.getByTestId('lang').textContent).toBe('vi');
    expect(screen.getByTestId('title').textContent).toBe('Chọn bài học');

    act(() => { screen.getByText('en').click(); });
    expect(screen.getByTestId('lang').textContent).toBe('en');
    expect(screen.getByTestId('title').textContent).toBe('Choose a lesson');
    expect(localStorage.getItem('dlearning.lang')).toBe('en');
  });
});
```

- [ ] **Step 2: Run to verify it fails**

Run: `npm test -- useI18n`
Expected: FAIL (cannot import `./LanguageProvider`).

- [ ] **Step 3: Write the catalogs**

Create `src/i18n/vi.json`:

```json
{
  "app.title": "dLearning",
  "app.chooseLesson": "Chọn bài học",
  "app.greeting": "Xin chào, {name} 👋",
  "common.comingSoon": "Sắp ra mắt",
  "common.logout": "Đăng xuất",
  "common.loading": "Đang tải…",
  "common.retry": "Thử lại",
  "common.error": "Không tải được dữ liệu.",
  "lang.vi": "Tiếng Việt",
  "lang.en": "English"
}
```

Create `src/i18n/en.json`:

```json
{
  "app.title": "dLearning",
  "app.chooseLesson": "Choose a lesson",
  "app.greeting": "Hi {name} 👋",
  "common.comingSoon": "Coming soon",
  "common.logout": "Log out",
  "common.loading": "Loading…",
  "common.retry": "Retry",
  "common.error": "Could not load data.",
  "lang.vi": "Tiếng Việt",
  "lang.en": "English"
}
```

- [ ] **Step 4: Write the context, provider, hook**

Create `src/presentation/i18n/LanguageContext.ts`:

```ts
import { createContext } from 'react';

export type Lang = 'en' | 'vi';

export interface LanguageContextValue {
  lang: Lang;
  setLang: (lang: Lang) => void;
  t: (key: string, vars?: Record<string, string>) => string;
}

export const LanguageContext = createContext<LanguageContextValue | null>(null);
```

Create `src/presentation/i18n/LanguageProvider.tsx`:

```tsx
import { useCallback, useMemo, useState, type ReactNode } from 'react';
import en from '../../i18n/en.json';
import vi from '../../i18n/vi.json';
import { LanguageContext, type Lang, type LanguageContextValue } from './LanguageContext';

const KEY = 'dlearning.lang';
const catalogs: Record<Lang, Record<string, string>> = { en, vi };

function initialLang(): Lang {
  const saved = localStorage.getItem(KEY);
  return saved === 'en' || saved === 'vi' ? saved : 'vi';
}

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [lang, setLangState] = useState<Lang>(initialLang);

  const setLang = useCallback((next: Lang) => {
    localStorage.setItem(KEY, next);
    setLangState(next);
  }, []);

  const t = useCallback(
    (key: string, vars?: Record<string, string>) => {
      let text = catalogs[lang][key] ?? key;
      if (vars) {
        for (const [k, v] of Object.entries(vars)) {
          text = text.replace(`{${k}}`, v);
        }
      }
      return text;
    },
    [lang],
  );

  const value = useMemo<LanguageContextValue>(() => ({ lang, setLang, t }), [lang, setLang, t]);

  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>;
}
```

Create `src/presentation/i18n/useI18n.ts`:

```ts
import { useContext } from 'react';
import { LanguageContext } from './LanguageContext';

export function useI18n() {
  const ctx = useContext(LanguageContext);
  if (!ctx) {
    throw new Error('useI18n must be used within a LanguageProvider');
  }
  return ctx;
}
```

- [ ] **Step 5: Enable JSON imports (tsconfig) if needed, then run the test**

Ensure `resolveJsonModule` is on (Vite's `tsconfig.app.json` extends `@tsconfig`/has it by default; if the import errors, add `"resolveJsonModule": true` to `tsconfig.app.json` `compilerOptions`).

Run: `npm test -- useI18n`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-web
git add src/i18n src/presentation/i18n tsconfig.app.json
git commit -m "feat: add i18n catalogs, LanguageProvider, and useI18n

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task B2: Catalog domain, repository port, use cases

**Files:**
- Create: `src/domain/models/Category.ts`, `src/domain/models/Product.ts`
- Create: `src/domain/repositories/CatalogRepository.ts`
- Create: `src/application/usecases/getCategories.ts`, `getProducts.ts`
- Create: `src/application/usecases/getCategories.test.ts`, `getProducts.test.ts`

**Interfaces:**
- Produces: `Category { code, name, iconKey, displayOrder }`; `Product { code, name, iconKey, displayOrder, isAvailable }`; `CatalogRepository { getCategories(lang): Promise<Category[]>; getProducts(categoryCode, lang): Promise<Product[]> }`; `makeGetCategories(repo)`, `makeGetProducts(repo)` factory fns.

- [ ] **Step 1: Write the failing tests**

Create `src/application/usecases/getCategories.test.ts`:

```ts
import { describe, it, expect, vi } from 'vitest';
import { makeGetCategories } from './getCategories';
import type { CatalogRepository } from '../../domain/repositories/CatalogRepository';

describe('getCategories use case', () => {
  it('passes the language through to the repository', async () => {
    const cats = [{ code: 'preschool', name: 'Mầm non', iconKey: '🧸', displayOrder: 1 }];
    const repo: CatalogRepository = {
      getCategories: vi.fn().mockResolvedValue(cats),
      getProducts: vi.fn(),
    };

    const getCategories = makeGetCategories(repo);
    const result = await getCategories('vi');

    expect(repo.getCategories).toHaveBeenCalledWith('vi');
    expect(result).toEqual(cats);
  });
});
```

Create `src/application/usecases/getProducts.test.ts`:

```ts
import { describe, it, expect, vi } from 'vitest';
import { makeGetProducts } from './getProducts';
import type { CatalogRepository } from '../../domain/repositories/CatalogRepository';

describe('getProducts use case', () => {
  it('passes category code and language to the repository', async () => {
    const products = [{ code: 'alphabet', name: 'Alphabet', iconKey: '🔤', displayOrder: 1, isAvailable: true }];
    const repo: CatalogRepository = {
      getCategories: vi.fn(),
      getProducts: vi.fn().mockResolvedValue(products),
    };

    const getProducts = makeGetProducts(repo);
    const result = await getProducts('preschool', 'en');

    expect(repo.getProducts).toHaveBeenCalledWith('preschool', 'en');
    expect(result).toEqual(products);
  });
});
```

- [ ] **Step 2: Run to verify they fail**

Run: `npm test -- getCategories getProducts`
Expected: FAIL (missing modules).

- [ ] **Step 3: Write the models, port, use cases**

Create `src/domain/models/Category.ts`:

```ts
export interface Category {
  code: string;
  name: string;
  iconKey: string;
  displayOrder: number;
}
```

Create `src/domain/models/Product.ts`:

```ts
export interface Product {
  code: string;
  name: string;
  iconKey: string;
  displayOrder: number;
  isAvailable: boolean;
}
```

Create `src/domain/repositories/CatalogRepository.ts`:

```ts
import type { Category } from '../models/Category';
import type { Product } from '../models/Product';

export type Lang = 'en' | 'vi';

export interface CatalogRepository {
  getCategories(lang: Lang): Promise<Category[]>;
  getProducts(categoryCode: string, lang: Lang): Promise<Product[]>;
}
```

Create `src/application/usecases/getCategories.ts`:

```ts
import type { Category } from '../../domain/models/Category';
import type { CatalogRepository, Lang } from '../../domain/repositories/CatalogRepository';

export function makeGetCategories(repo: CatalogRepository) {
  return (lang: Lang): Promise<Category[]> => repo.getCategories(lang);
}
```

Create `src/application/usecases/getProducts.ts`:

```ts
import type { Product } from '../../domain/models/Product';
import type { CatalogRepository, Lang } from '../../domain/repositories/CatalogRepository';

export function makeGetProducts(repo: CatalogRepository) {
  return (categoryCode: string, lang: Lang): Promise<Product[]> => repo.getProducts(categoryCode, lang);
}
```

- [ ] **Step 4: Run to verify they pass**

Run: `npm test -- getCategories getProducts`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/domain/models/Category.ts src/domain/models/Product.ts src/domain/repositories/CatalogRepository.ts src/application/usecases/getCategories.ts src/application/usecases/getProducts.ts src/application/usecases/getCategories.test.ts src/application/usecases/getProducts.test.ts
git commit -m "feat: add catalog domain models, port, and use cases

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task B3: HttpClient query params + ApiCatalogRepository + DI

**Files:**
- Modify: `src/infrastructure/http/HttpClient.ts`
- Create: `src/infrastructure/repositories/ApiCatalogRepository.ts`
- Modify: `src/di/container.ts`

**Interfaces:**
- Consumes: `HttpClient` (extend `get` to accept an optional query object).
- Produces: `ApiCatalogRepository implements CatalogRepository`; `container.getCategories`, `container.getProducts`.

- [ ] **Step 1: Extend `HttpClient.get` with query params**

In `src/infrastructure/http/HttpClient.ts`, replace the `get` method and add a query-string helper:

```ts
  get<T>(path: string, query?: Record<string, string>): Promise<T> {
    const qs = query ? `?${new URLSearchParams(query).toString()}` : '';
    return this.request<T>('GET', `${path}${qs}`);
  }
```

(Leave `post` and `request` unchanged.)

- [ ] **Step 2: Write the API catalog repository**

Create `src/infrastructure/repositories/ApiCatalogRepository.ts`:

```ts
import type { Category } from '../../domain/models/Category';
import type { Product } from '../../domain/models/Product';
import type { CatalogRepository, Lang } from '../../domain/repositories/CatalogRepository';
import type { HttpClient } from '../http/HttpClient';

export class ApiCatalogRepository implements CatalogRepository {
  private readonly http: HttpClient;

  constructor(http: HttpClient) {
    this.http = http;
  }

  getCategories(lang: Lang): Promise<Category[]> {
    return this.http.get<Category[]>('/categories', { lang });
  }

  getProducts(categoryCode: string, lang: Lang): Promise<Product[]> {
    return this.http.get<Product[]>(`/categories/${categoryCode}/products`, { lang });
  }
}
```

- [ ] **Step 3: Wire the container**

In `src/di/container.ts`, add imports and registrations:

```ts
import { makeGetCategories } from '../application/usecases/getCategories';
import { makeGetProducts } from '../application/usecases/getProducts';
import { ApiCatalogRepository } from '../infrastructure/repositories/ApiCatalogRepository';
```

Inside `createContainer`, after the existing repositories:

```ts
  const catalogRepository = new ApiCatalogRepository(http);
```

And in the returned object, add:

```ts
    getCategories: makeGetCategories(catalogRepository),
    getProducts: makeGetProducts(catalogRepository),
```

- [ ] **Step 4: Type-check**

Run: `npx tsc -b`
Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add src/infrastructure/http/HttpClient.ts src/infrastructure/repositories/ApiCatalogRepository.ts src/di/container.ts
git commit -m "feat: add ApiCatalogRepository and wire catalog use cases

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task B4: LanguageSwitcher + wrap app in LanguageProvider

**Files:**
- Create: `src/presentation/components/LanguageSwitcher.tsx`
- Modify: `src/main.tsx`

**Interfaces:**
- Consumes: `useI18n`.
- Produces: `LanguageSwitcher` (VI/EN toggle); app tree wrapped in `LanguageProvider`.

- [ ] **Step 1: Write the switcher**

Create `src/presentation/components/LanguageSwitcher.tsx`:

```tsx
import { useI18n } from '../i18n/useI18n';
import type { Lang } from '../i18n/LanguageContext';

export function LanguageSwitcher() {
  const { lang, setLang } = useI18n();
  const options: Lang[] = ['vi', 'en'];
  return (
    <div style={{ display: 'inline-flex', border: '1px solid #E4E2EC', borderRadius: 10, overflow: 'hidden', background: '#fff' }}>
      {options.map((code) => (
        <button
          key={code}
          onClick={() => setLang(code)}
          style={{
            border: 'none',
            cursor: 'pointer',
            padding: '6px 12px',
            fontSize: 13,
            fontWeight: 600,
            background: lang === code ? '#4F46E5' : 'transparent',
            color: lang === code ? '#fff' : '#6B6B80',
          }}
        >
          {code.toUpperCase()}
        </button>
      ))}
    </div>
  );
}
```

- [ ] **Step 2: Wrap the app in `LanguageProvider`**

In `src/main.tsx`, add the import and wrap `AuthProvider` (LanguageProvider outermost so auth pages can translate):

```tsx
import { LanguageProvider } from './presentation/i18n/LanguageProvider';
```

Change the render tree to:

```tsx
  <React.StrictMode>
    <LanguageProvider>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </LanguageProvider>
  </React.StrictMode>,
```

- [ ] **Step 3: Type-check + tests**

Run: `npx tsc -b && npm test`
Expected: build clean; all tests green.

- [ ] **Step 4: Commit**

```bash
git add src/presentation/components/LanguageSwitcher.tsx src/main.tsx
git commit -m "feat: add LanguageSwitcher and wrap app in LanguageProvider

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task B5: HomePage menu + routing

**Files:**
- Create: `src/presentation/pages/HomePage.tsx`
- Create: `src/presentation/components/CategoryTabs.tsx`
- Create: `src/presentation/components/ProductCard.tsx`
- Modify: `src/presentation/router.tsx`

**Interfaces:**
- Consumes: `container.getCategories`, `container.getProducts`, `useAuth`, `useI18n`, `LanguageSwitcher`.
- Produces: `HomePage` at `/`; `*` redirects to `/`.

- [ ] **Step 1: Write `CategoryTabs`**

Create `src/presentation/components/CategoryTabs.tsx`:

```tsx
import type { Category } from '../../domain/models/Category';

interface CategoryTabsProps {
  categories: Category[];
  selected: string;
  onSelect: (code: string) => void;
}

export function CategoryTabs({ categories, selected, onSelect }: CategoryTabsProps) {
  return (
    <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap', marginBottom: 22 }}>
      {categories.map((c) => {
        const active = c.code === selected;
        return (
          <button
            key={c.code}
            onClick={() => onSelect(c.code)}
            style={{
              border: 'none',
              cursor: 'pointer',
              padding: '10px 16px',
              borderRadius: 999,
              fontFamily: 'Sora',
              fontWeight: 700,
              fontSize: 14,
              display: 'flex',
              alignItems: 'center',
              gap: 8,
              background: active ? 'linear-gradient(135deg,#4F46E5,#6D28D9)' : '#fff',
              color: active ? '#fff' : '#5A5A6B',
              boxShadow: active ? '0 10px 22px -12px rgba(79,70,229,.7)' : 'none',
            }}
          >
            <span>{c.iconKey}</span>
            {c.name}
          </button>
        );
      })}
    </div>
  );
}
```

- [ ] **Step 2: Write `ProductCard`**

Create `src/presentation/components/ProductCard.tsx`:

```tsx
import type { Product } from '../../domain/models/Product';
import { letterCovers } from '../../styles/tokens';

interface ProductCardProps {
  product: Product;
  index: number;
  comingSoonLabel: string;
  onOpen: () => void;
}

export function ProductCard({ product, index, comingSoonLabel, onOpen }: ProductCardProps) {
  const cover = letterCovers[index % letterCovers.length];
  const disabled = !product.isAvailable;
  return (
    <button
      onClick={disabled ? undefined : onOpen}
      disabled={disabled}
      style={{
        position: 'relative',
        textAlign: 'left',
        border: '1px solid rgba(27,27,47,.06)',
        borderRadius: 18,
        overflow: 'hidden',
        cursor: disabled ? 'default' : 'pointer',
        background: '#fff',
        padding: 0,
        opacity: disabled ? 0.72 : 1,
      }}
    >
      <div style={{ height: 96, background: cover, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 44 }}>
        {product.iconKey}
      </div>
      <div style={{ padding: '14px 16px 16px' }}>
        <div style={{ fontFamily: 'Sora', fontWeight: 700, fontSize: 15 }}>{product.name}</div>
        {disabled && (
          <div style={{ marginTop: 6, display: 'inline-block', fontSize: 11, fontWeight: 700, color: '#8A8A99', background: '#F1F0F6', borderRadius: 999, padding: '3px 9px' }}>
            {comingSoonLabel}
          </div>
        )}
      </div>
    </button>
  );
}
```

- [ ] **Step 3: Write `HomePage`**

Create `src/presentation/pages/HomePage.tsx`:

```tsx
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { container } from '../../di/container';
import type { Category } from '../../domain/models/Category';
import type { Product } from '../../domain/models/Product';
import { CategoryTabs } from '../components/CategoryTabs';
import { LanguageSwitcher } from '../components/LanguageSwitcher';
import { ProductCard } from '../components/ProductCard';
import { useAuth } from '../auth/useAuth';
import { useI18n } from '../i18n/useI18n';

type Status = 'loading' | 'ready' | 'error';

export function HomePage() {
  const { session, logout } = useAuth();
  const { lang, t } = useI18n();
  const navigate = useNavigate();

  const [categories, setCategories] = useState<Category[]>([]);
  const [selected, setSelected] = useState<string>('preschool');
  const [products, setProducts] = useState<Product[]>([]);
  const [status, setStatus] = useState<Status>('loading');
  const [reloadKey, setReloadKey] = useState(0);

  // Categories (the menu) reload only when the language changes.
  useEffect(() => {
    let active = true;
    container.getCategories(lang)
      .then((cats) => { if (active) setCategories(cats); })
      .catch(() => { if (active) setStatus('error'); });
    return () => { active = false; };
  }, [lang]);

  // Products reload when the selected category, language, or retry counter changes.
  useEffect(() => {
    let active = true;
    setStatus('loading');
    container.getProducts(selected, lang)
      .then((prods) => { if (active) { setProducts(prods); setStatus('ready'); } })
      .catch(() => { if (active) setStatus('error'); });
    return () => { active = false; };
  }, [selected, lang, reloadKey]);

  function openProduct(product: Product) {
    if (product.code === 'alphabet') {
      navigate('/alphabet');
    }
  }

  function handleLogout() {
    logout();
    navigate('/login');
  }

  return (
    <div style={{ minHeight: '100vh', background: '#F4F3EF' }}>
      <header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '22px 34px', position: 'sticky', top: 0, background: 'rgba(244,243,239,.88)', backdropFilter: 'blur(12px)', zIndex: 5 }}>
        <div style={{ fontFamily: 'Sora', fontWeight: 800, fontSize: 20 }}>{t('app.title')}</div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
          <LanguageSwitcher />
          <button onClick={handleLogout} style={{ border: '1px solid rgba(27,27,47,.08)', background: '#fff', borderRadius: 12, padding: '10px 16px', cursor: 'pointer', fontWeight: 600, fontSize: 13, color: '#5A5A6B' }}>
            {t('common.logout')}
          </button>
        </div>
      </header>

      <main style={{ padding: '6px 34px 60px' }}>
        <h1 style={{ fontFamily: 'Sora', fontWeight: 800, fontSize: 26, letterSpacing: '-.02em', margin: '0 0 4px' }}>
          {t('app.greeting', { name: session?.user.displayName ?? '' })}
        </h1>
        <p style={{ fontSize: 14, color: '#6B6B80', margin: '0 0 22px' }}>{t('app.chooseLesson')}</p>

        {categories.length > 0 && (
          <CategoryTabs categories={categories} selected={selected} onSelect={setSelected} />
        )}

        {status === 'loading' && <p style={{ color: '#6B6B80' }}>{t('common.loading')}</p>}

        {status === 'error' && (
          <div style={{ background: '#FEF2F2', border: '1px solid #FECACA', color: '#B91C1C', padding: '12px 14px', borderRadius: 12, display: 'inline-flex', gap: 12, alignItems: 'center' }}>
            {t('common.error')}
            <button onClick={() => setReloadKey((k) => k + 1)} style={{ border: 'none', background: '#B91C1C', color: '#fff', borderRadius: 8, padding: '6px 12px', cursor: 'pointer' }}>{t('common.retry')}</button>
          </div>
        )}

        {status === 'ready' && (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(160px,1fr))', gap: 16 }}>
            {products.length === 0
              ? <p style={{ color: '#8A8A99' }}>{t('common.comingSoon')}</p>
              : products.map((p, i) => (
                  <ProductCard key={p.code} product={p} index={i} comingSoonLabel={t('common.comingSoon')} onOpen={() => openProduct(p)} />
                ))}
          </div>
        )}
      </main>
    </div>
  );
}
```

- [ ] **Step 4: Update the router**

Replace `src/presentation/router.tsx`:

```tsx
import { createBrowserRouter, Navigate } from 'react-router-dom';
import { RequireAuth } from './auth/RequireAuth';
import { AlphabetPage } from './pages/AlphabetPage';
import { HomePage } from './pages/HomePage';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  { path: '/register', element: <RegisterPage /> },
  {
    path: '/',
    element: (
      <RequireAuth>
        <HomePage />
      </RequireAuth>
    ),
  },
  {
    path: '/alphabet',
    element: (
      <RequireAuth>
        <AlphabetPage />
      </RequireAuth>
    ),
  },
  { path: '*', element: <Navigate to="/" replace /> },
]);
```

- [ ] **Step 5: Point post-login/register navigation at `/`**

In `src/presentation/pages/LoginPage.tsx` and `src/presentation/pages/RegisterPage.tsx`, change `navigate('/alphabet')` to `navigate('/')` (the home menu is now the landing screen).

- [ ] **Step 6: Build + tests**

Run: `npm run build && npm test`
Expected: `tsc -b` + `vite build` succeed; all Vitest tests green.

- [ ] **Step 7: Commit**

```bash
git add src/presentation/pages/HomePage.tsx src/presentation/components/CategoryTabs.tsx src/presentation/components/ProductCard.tsx src/presentation/router.tsx src/presentation/pages/LoginPage.tsx src/presentation/pages/RegisterPage.tsx
git commit -m "feat: add home menu page (categories + products) and route to it after login

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Task B6: Frontend Dockerfile + nginx

**Files:**
- Create: `dlearning-web/nginx.conf`
- Create: `dlearning-web/Dockerfile`
- Create: `dlearning-web/.dockerignore`

- [ ] **Step 1: Write `.dockerignore`**

Create `dlearning-web/.dockerignore`:

```
node_modules/
dist/
.git/
```

- [ ] **Step 2: Write `nginx.conf`**

Create `dlearning-web/nginx.conf`:

```nginx
server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    location /api/ {
        proxy_pass http://backend:8080/;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

- [ ] **Step 3: Write the Dockerfile**

Create `dlearning-web/Dockerfile`:

```dockerfile
# syntax=docker/dockerfile:1
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

- [ ] **Step 4: Build the image**

Run: `cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-web && docker build -t dlearning-web:local .`
Expected: image builds successfully.

- [ ] **Step 5: Commit**

```bash
git add Dockerfile .dockerignore nginx.conf
git commit -m "chore: add frontend Dockerfile with nginx and /api proxy

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

# Phase C — Full-stack deployment (`orchestrator`)

## Task C1: Full-stack docker-compose + run

**Files:**
- Create: `orchestrator/deploy/docker-compose.yml`
- Create: `orchestrator/deploy/README.md`

**Interfaces:**
- Consumes: `dlearning-be/Dockerfile`, `dlearning-web/Dockerfile` (sibling repos).
- Produces: one-command full stack at `http://localhost:8080`.

- [ ] **Step 1: Write the compose file**

Create `orchestrator/deploy/docker-compose.yml`:

```yaml
name: dlearning

services:
  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: dlearning
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    volumes:
      - dlearning_pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d dlearning"]
      interval: 5s
      timeout: 5s
      retries: 10

  backend:
    build:
      context: ../../dlearning-be
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__Database: Host=postgres;Port=5432;Database=dlearning;Username=postgres;Password=postgres
      Jwt__Secret: dlearning-super-secret-signing-key-change-me-1234567890
      Jwt__Issuer: dlearning
      Jwt__Audience: dlearning-web
      Jwt__ExpirationInMinutes: "60"
    depends_on:
      postgres:
        condition: service_healthy

  frontend:
    build:
      context: ../../dlearning-web
    ports:
      - "8080:80"
    depends_on:
      - backend

volumes:
  dlearning_pgdata:
```

Notes: the backend runs `Development` so `ApplyMigrations()` creates the schema + seeds (users, alphabet, categories, products) on start. The frontend's nginx proxies `/api` → `backend:8080`, so the SPA's `/api` calls work with no CORS. CORS in `Program.cs` still allows `http://localhost:5173` for local dev — harmless here.

- [ ] **Step 2: Write a short deploy README**

Create `orchestrator/deploy/README.md`:

```markdown
# dLearning full-stack deploy

Builds the two sibling repos and Postgres into one stack.

```bash
cd orchestrator/deploy
docker compose up --build        # → http://localhost:8080
docker compose down              # stop (add -v to wipe the DB volume)
```

- Web (nginx) on `:8080`, proxying `/api` → backend `:8080` (in-network).
- Backend runs `ASPNETCORE_ENVIRONMENT=Development`, so it auto-applies EF migrations
  and seeds the demo user, alphabet, categories and products on first start.
- Requires `dlearning-be/` and `dlearning-web/` present as siblings of `orchestrator/`.
```

- [ ] **Step 3: Bring the stack up and verify end-to-end**

Run:
```bash
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/orchestrator/deploy
docker compose up --build -d
# wait for backend to migrate + serve
for i in $(seq 1 60); do curl -s -o /dev/null "http://localhost:8080/api/categories" && break; sleep 2; done
```

Verify through the frontend's nginx proxy:
```bash
curl -s "http://localhost:8080/api/categories?lang=vi" | grep -o '"name":"[^"]*"' | head -1      # → "name":"Mầm non"
curl -s "http://localhost:8080/api/categories?lang=en" | grep -o '"name":"[^"]*"' | head -1      # → "name":"Preschool"
curl -s "http://localhost:8080/api/categories/preschool/products?lang=en" | grep -o '"code"' | wc -l  # → 4
curl -s -o /dev/null -w "%{http_code}\n" "http://localhost:8080/"                                 # → 200 (SPA)
# login still works end-to-end through the proxy:
curl -s -X POST "http://localhost:8080/api/users/login" -H 'Content-Type: application/json' -d '{"identifier":"demo@dlearning.vn","password":"Demo@123"}' | grep -o '"token"'  # → "token"
```
Expected: the values in the comments above. Then `docker compose down`.

- [ ] **Step 4: Commit**

```bash
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/orchestrator
git add deploy/ 2>/dev/null || true
git commit -m "chore: add full-stack docker-compose for dLearning" 2>/dev/null || echo "orchestrator not a git repo yet — files created, commit when initialized"
```

(If the orchestrator isn't a git repo, the compose files still exist and work; initialize/commit later.)

---

# Self-Review (coverage map)

- **Spec §2 i18n strategy** (resx for names, JSON for UI, `?lang=`→`Accept-Language`, default vi) → A4, B1.
- **Spec §3.1 Catalog slice** (Category/Product structural-only) → A1; queries/handlers/DbSets → A2; EF config + seed + migration → A3.
- **Spec §3.1 Web.Api localization** (AddLocalization, RequestLocalization, resx) → A4.
- **Spec §3.2 endpoints** (anonymous GET /categories, /categories/{code}/products) → A5.
- **Spec §3.3 backend tests** (unit + integration + arch stays green) → A2, A6.
- **Spec §4.1 FE i18n** (catalogs, provider, hook, switcher, localStorage) → B1, B4.
- **Spec §4.2 FE clean-arch pieces** (models, port, use cases, ApiCatalogRepository, DI) → B2, B3.
- **Spec §4.3 menu/home page** (`/`, tabs, cards, alphabet→screen, coming-soon, `*`→`/`) → B5.
- **Spec §4.4 FE tests** → B1, B2.
- **Spec §5.1 backend Dockerfile** → A7. **§5.2 frontend Dockerfile + nginx** → B6. **§5.3 full-stack compose** → C1.
- **Spec §6 run it** → C1 Step 3.
