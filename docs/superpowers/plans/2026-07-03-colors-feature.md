# dLearning — Colors (Màu sắc) Feature Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a JWT-protected `GET /colors` API returning 11 localized colors and a `/colors` learning screen (swatch grid → detail modal), turning the preschool "Colors" product from "coming soon" into a working lesson.

**Architecture:** A new `Colors` vertical slice in the backend mirrors the `Alphabets` slice (reference-data entity + query + EF seed), but color **names and example words are localized via `.resx`** (like the Catalog) rather than stored in the DB. The frontend adds a `Colors` clean-arch stack (model → port → use case → API repo → page) mirroring the alphabet screen, and generalizes the home menu so any available product opens `/<code>`.

**Tech Stack:** .NET 10, EF Core 10, `Microsoft.Extensions.Localization` (shared framework), Vite + React 19 + TypeScript, Vitest, Docker Compose.

## Global Constraints

- **Backend repo:** `/Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be` (`main`). **Frontend repo:** `/Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-web` (`main`). **Deploy compose:** `/Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/orchestrator/deploy`. Commit in the repo the changed files belong to. Commits are now authored as `datnm555 <mydatng@gmail.com>` (already configured).
- **Central Package Management** — no `Version=` on `<PackageReference>`. No new NuGet packages needed.
- **`TreatWarningsAsErrors` + `AnalysisMode=All` + `EnforceCodeStyleInBuild`** — analyzer/style violations fail the build. File-scoped namespaces, braces always, `I`-prefixed interfaces; use `CultureInfo.InvariantCulture` for the hex-suffix formatting in seeds.
- **Dependency rule** `SharedKernel → Domain → Application → Infrastructure → Web.Api`, enforced by `tests/ArchitectureTests`. **Application must NOT reference `Microsoft.AspNetCore`/localization** — localization stays in Web.Api. Handlers are `internal sealed` and implement `IQueryHandler<,>`.
- **Feature folders plural, aggregate singular** (`Colors/`, `Color`). Result pattern; endpoints end with `result.ToHttpResult(...)`. Reference entities `builder.Ignore(x => x.DomainEvents)`.
- **EF migrations** live in `src/Infrastructure/Database/Migrations/` (`-o Database/Migrations`); run `dotnet ef` with `ASPNETCORE_ENVIRONMENT=Production`. Migration files are analyzer-exempt via `.editorconfig`.
- **The single Docker DB** is the deploy stack's postgres (`orchestrator/deploy`, project `dlearning`); its port is NOT published to the host. For `dotnet ef` / local `dotnet run`, start a scratch DB with `cd dlearning-be && docker compose up -d` (dev postgres on 5432), and stop it after with `docker compose down` — or apply the migration by rebuilding the deploy stack (auto-migrate on start).
- **Supported cultures `en`, `vi`; default `vi`** (existing request-localization middleware). Culture from `?lang=` then `Accept-Language`.
- **Color codes:** `red, orange, yellow, green, blue, purple, pink, brown, black, white, gray`. **Static seed GUIDs:** colors `c0100000-0000-4000-8000-0000000000NN` (NN = display order in hex, 01..0b).
- **Frontend:** `erasableSyntaxOnly` is ON — no constructor parameter properties; declare fields and assign in the body. Dependency direction `presentation → application → domain`, `infrastructure → domain`, `di` composes. Pass the current `lang` from `useI18n()` to the API.
- **API contract:** `GET /colors?lang=` is **JWT-protected** (like `/alphabet`). Both dev proxy and nginx map `/api` → backend root.

---

# Phase A — Backend Colors slice (`dlearning-be`)

## Task A1: `Color` domain entity

**Files:**
- Create: `src/Domain/Colors/Color.cs`
- Test: `tests/Application.UnitTests/Colors/ColorTests.cs`

**Interfaces:**
- Produces: `Color : Entity` with `init` props `Code`, `HexValue`, `ExampleEmoji`, `DisplayOrder` (mirrors `AlphabetLetter`; object-initializer construction, no factory).

- [ ] **Step 1: Write the failing test**

Create `tests/Application.UnitTests/Colors/ColorTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~ColorTests"`
Expected: FAIL (build error — `Domain.Colors.Color` does not exist).

- [ ] **Step 3: Write the entity**

Create `src/Domain/Colors/Color.cs`:

```csharp
using SharedKernel;

namespace Domain.Colors;

public sealed class Color : Entity
{
    public string Code { get; init; } = string.Empty;

    public string HexValue { get; init; } = string.Empty;

    public string ExampleEmoji { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~ColorTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be
git add src/Domain/Colors tests/Application.UnitTests/Colors/ColorTests.cs
git commit -m "feat: add Color domain entity"
```

---

## Task A2: `GetColorsQuery` + handler + DbSet

**Files:**
- Create: `src/Application/Colors/Data/ColorDto.cs`
- Create: `src/Application/Colors/GetColorsQuery.cs`
- Create: `src/Application/Colors/GetColorsQueryHandler.cs`
- Modify: `src/Application/Abstractions/Data/IApplicationDbContext.cs`
- Modify: `src/Infrastructure/Database/ApplicationDbContext.cs`
- Modify: `src/Application/DependencyInjection.cs`
- Test: `tests/Application.UnitTests/Colors/GetColorsQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `IApplicationDbContext` (add `Colors` DbSet), `Color`.
- Produces: `ColorDto(string Code, string HexValue, string ExampleEmoji, int DisplayOrder)`; `GetColorsQuery() : IQuery<IReadOnlyList<ColorDto>>`; handler ordered by `DisplayOrder`.

- [ ] **Step 1: Write the failing test**

Create `tests/Application.UnitTests/Colors/GetColorsQueryHandlerTests.cs`:

```csharp
using Application.Abstractions.Data;
using Application.Colors;
using Domain.Colors;
using MockQueryable.NSubstitute;
using NSubstitute;
using Shouldly;

namespace Application.UnitTests.Colors;

public class GetColorsQueryHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();

    [Fact]
    public async Task Handle_ReturnsColorsOrderedByDisplayOrder()
    {
        var colors = new List<Color>
        {
            new() { Code = "blue", HexValue = "#3B82F6", ExampleEmoji = "🫐", DisplayOrder = 5 },
            new() { Code = "red", HexValue = "#EF4444", ExampleEmoji = "🍎", DisplayOrder = 1 }
        };
        var set = colors.BuildMockDbSet();
        _dbContext.Colors.Returns(set);
        var handler = new GetColorsQueryHandler(_dbContext);

        var result = await handler.Handle(new GetColorsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].Code.ShouldBe("red");
        result.Value[1].Code.ShouldBe("blue");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~GetColorsQueryHandlerTests"`
Expected: FAIL (build error — query types don't exist).

- [ ] **Step 3: Write DTO, query, handler**

Create `src/Application/Colors/Data/ColorDto.cs`:

```csharp
namespace Application.Colors.Data;

public sealed record ColorDto(string Code, string HexValue, string ExampleEmoji, int DisplayOrder);
```

Create `src/Application/Colors/GetColorsQuery.cs`:

```csharp
using Application.Abstractions.Messaging;
using Application.Colors.Data;

namespace Application.Colors;

public sealed record GetColorsQuery : IQuery<IReadOnlyList<ColorDto>>;
```

Create `src/Application/Colors/GetColorsQueryHandler.cs`:

```csharp
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Colors.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Colors;

internal sealed class GetColorsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetColorsQuery, IReadOnlyList<ColorDto>>
{
    public async Task<Result<IReadOnlyList<ColorDto>>> Handle(
        GetColorsQuery query,
        CancellationToken cancellationToken)
    {
        List<ColorDto> colors = await dbContext.Colors
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ColorDto(c.Code, c.HexValue, c.ExampleEmoji, c.DisplayOrder))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ColorDto>>(colors);
    }
}
```

- [ ] **Step 4: Expose the DbSet and register the handler**

In `src/Application/Abstractions/Data/IApplicationDbContext.cs`, add `using Domain.Colors;` and the DbSet (place after `Products`):

```csharp
    DbSet<Color> Colors { get; }
```

In `src/Infrastructure/Database/ApplicationDbContext.cs`, add `using Domain.Colors;` and the property (after `Products`):

```csharp
    public DbSet<Color> Colors => Set<Color>();
```

In `src/Application/DependencyInjection.cs`, add usings `Application.Colors;`, `Application.Colors.Data;` and register:

```csharp
        services.AddScoped<IQueryHandler<GetColorsQuery, IReadOnlyList<ColorDto>>, GetColorsQueryHandler>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~Colors"`
Expected: PASS (ColorTests + GetColorsQueryHandlerTests green).

- [ ] **Step 6: Commit**

```bash
git add src/Application/Colors src/Application/Abstractions/Data/IApplicationDbContext.cs src/Infrastructure/Database/ApplicationDbContext.cs src/Application/DependencyInjection.cs tests/Application.UnitTests/Colors/GetColorsQueryHandlerTests.cs
git commit -m "feat: add GetColors query, handler, and Colors DbSet"
```

---

## Task A3: EF config + seed + product flip + migration

**Files:**
- Create: `src/Infrastructure/Colors/ColorConfiguration.cs`
- Modify: `src/Infrastructure/Catalog/ProductConfiguration.cs` (flip `colors` → `IsAvailable = true`)
- Create: `src/Infrastructure/Database/Migrations/*_Colors.cs` (generated)

**Interfaces:**
- Produces: `colors` table + 11-row seed; the seeded `colors` product row updated to `IsAvailable = true`.

- [ ] **Step 1: Write the color configuration + seed**

Create `src/Infrastructure/Colors/ColorConfiguration.cs`:

```csharp
using System.Globalization;
using Domain.Colors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Colors;

internal sealed class ColorConfiguration : IEntityTypeConfiguration<Color>
{
    public void Configure(EntityTypeBuilder<Color> builder)
    {
        builder.ToTable("colors");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).HasMaxLength(32).IsRequired();
        builder.Property(c => c.HexValue).HasMaxLength(9).IsRequired();
        builder.Property(c => c.ExampleEmoji).HasMaxLength(16).IsRequired();
        builder.Property(c => c.DisplayOrder).IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();
        builder.Ignore(c => c.DomainEvents);

        builder.HasData(Seed());
    }

    private static object[] Seed()
    {
        (string Code, string Hex, string Emoji)[] rows =
        [
            ("red", "#EF4444", "🍎"),
            ("orange", "#F97316", "🍊"),
            ("yellow", "#FACC15", "🍌"),
            ("green", "#22C55E", "🍃"),
            ("blue", "#3B82F6", "🫐"),
            ("purple", "#A855F7", "🍇"),
            ("pink", "#EC4899", "🌸"),
            ("brown", "#92573B", "🍫"),
            ("black", "#1F2937", "🐈‍⬛"),
            ("white", "#F9FAFB", "☁️"),
            ("gray", "#9CA3AF", "🐘")
        ];

        var seed = new object[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            int order = i + 1;
            string hex = order.ToString("x2", CultureInfo.InvariantCulture);
            seed[i] = new
            {
                Id = new Guid("c0100000-0000-4000-8000-0000000000" + hex),
                Code = rows[i].Code,
                HexValue = rows[i].Hex,
                ExampleEmoji = rows[i].Emoji,
                DisplayOrder = order
            };
        }

        return seed;
    }
}
```

- [ ] **Step 2: Flip the `colors` product to available**

In `src/Infrastructure/Catalog/ProductConfiguration.cs`, change the `colors` row in the `Seed()` rows array so it is available:

```csharp
            ("alphabet", "🔤", true),
            ("animals", "🐾", false),
            ("colors", "🎨", true),
            ("counting", "🔢", false)
```

(Only the `colors` tuple's third value changes `false` → `true`.)

- [ ] **Step 3: Build**

Run: `cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be && dotnet build DLearning.slnx`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Start a scratch DB, create + apply the migration**

Run:
```bash
docker compose up -d
dotnet tool restore
ASPNETCORE_ENVIRONMENT=Production dotnet ef migrations add Colors --project src/Infrastructure --startup-project src/Web.Api -o Database/Migrations
ASPNETCORE_ENVIRONMENT=Production dotnet ef database update --project src/Infrastructure --startup-project src/Web.Api
```
Expected: `Done.` then `Applying migration '..._Colors'. Done.` The migration's `Up()` creates the `colors` table, inserts 11 rows, and emits an `UpdateData` setting the `colors` product's `IsAvailable` to `true`.

- [ ] **Step 5: Verify the seed + product flip**

Run:
```bash
docker exec dlearning-postgres psql -U postgres -d dlearning -t -c 'select "Code","HexValue" from colors order by "DisplayOrder";'
docker exec dlearning-postgres psql -U postgres -d dlearning -t -c $'select "Code","IsAvailable" from products where "Code"=\'colors\';'
```
Expected: 11 colors (`red` … `gray`); `colors | t`.

- [ ] **Step 6: Stop the scratch DB**

Run: `docker compose down`
(The deploy stack DB is separate and untouched; this only removes the dev scratch postgres.)

- [ ] **Step 7: Commit**

```bash
git add src/Infrastructure/Colors src/Infrastructure/Catalog/ProductConfiguration.cs src/Infrastructure/Database/Migrations
git commit -m "feat: add colors EF config + seed and mark colors product available"
```

---

## Task A4: Localization keys for colors

**Files:**
- Modify: `src/Web.Api/Resources/SharedResource.en.resx`
- Modify: `src/Web.Api/Resources/SharedResource.vi.resx`

**Interfaces:**
- Produces: resx keys `Color.<code>` (name) and `ColorExample.<code>` (example word) for all 11 colors, in both cultures.

- [ ] **Step 1: Add English color keys**

In `src/Web.Api/Resources/SharedResource.en.resx`, add these `<data>` entries before the closing `</root>`:

```xml
  <data name="Color.red" xml:space="preserve"><value>Red</value></data>
  <data name="Color.orange" xml:space="preserve"><value>Orange</value></data>
  <data name="Color.yellow" xml:space="preserve"><value>Yellow</value></data>
  <data name="Color.green" xml:space="preserve"><value>Green</value></data>
  <data name="Color.blue" xml:space="preserve"><value>Blue</value></data>
  <data name="Color.purple" xml:space="preserve"><value>Purple</value></data>
  <data name="Color.pink" xml:space="preserve"><value>Pink</value></data>
  <data name="Color.brown" xml:space="preserve"><value>Brown</value></data>
  <data name="Color.black" xml:space="preserve"><value>Black</value></data>
  <data name="Color.white" xml:space="preserve"><value>White</value></data>
  <data name="Color.gray" xml:space="preserve"><value>Gray</value></data>
  <data name="ColorExample.red" xml:space="preserve"><value>apple</value></data>
  <data name="ColorExample.orange" xml:space="preserve"><value>orange</value></data>
  <data name="ColorExample.yellow" xml:space="preserve"><value>banana</value></data>
  <data name="ColorExample.green" xml:space="preserve"><value>leaf</value></data>
  <data name="ColorExample.blue" xml:space="preserve"><value>blueberry</value></data>
  <data name="ColorExample.purple" xml:space="preserve"><value>grapes</value></data>
  <data name="ColorExample.pink" xml:space="preserve"><value>flower</value></data>
  <data name="ColorExample.brown" xml:space="preserve"><value>chocolate</value></data>
  <data name="ColorExample.black" xml:space="preserve"><value>black cat</value></data>
  <data name="ColorExample.white" xml:space="preserve"><value>cloud</value></data>
  <data name="ColorExample.gray" xml:space="preserve"><value>elephant</value></data>
```

- [ ] **Step 2: Add Vietnamese color keys**

In `src/Web.Api/Resources/SharedResource.vi.resx`, add before `</root>`:

```xml
  <data name="Color.red" xml:space="preserve"><value>Đỏ</value></data>
  <data name="Color.orange" xml:space="preserve"><value>Cam</value></data>
  <data name="Color.yellow" xml:space="preserve"><value>Vàng</value></data>
  <data name="Color.green" xml:space="preserve"><value>Xanh lá</value></data>
  <data name="Color.blue" xml:space="preserve"><value>Xanh dương</value></data>
  <data name="Color.purple" xml:space="preserve"><value>Tím</value></data>
  <data name="Color.pink" xml:space="preserve"><value>Hồng</value></data>
  <data name="Color.brown" xml:space="preserve"><value>Nâu</value></data>
  <data name="Color.black" xml:space="preserve"><value>Đen</value></data>
  <data name="Color.white" xml:space="preserve"><value>Trắng</value></data>
  <data name="Color.gray" xml:space="preserve"><value>Xám</value></data>
  <data name="ColorExample.red" xml:space="preserve"><value>quả táo</value></data>
  <data name="ColorExample.orange" xml:space="preserve"><value>quả cam</value></data>
  <data name="ColorExample.yellow" xml:space="preserve"><value>quả chuối</value></data>
  <data name="ColorExample.green" xml:space="preserve"><value>lá cây</value></data>
  <data name="ColorExample.blue" xml:space="preserve"><value>quả việt quất</value></data>
  <data name="ColorExample.purple" xml:space="preserve"><value>quả nho</value></data>
  <data name="ColorExample.pink" xml:space="preserve"><value>bông hoa</value></data>
  <data name="ColorExample.brown" xml:space="preserve"><value>sô cô la</value></data>
  <data name="ColorExample.black" xml:space="preserve"><value>mèo đen</value></data>
  <data name="ColorExample.white" xml:space="preserve"><value>đám mây</value></data>
  <data name="ColorExample.gray" xml:space="preserve"><value>con voi</value></data>
```

- [ ] **Step 3: Build**

Run: `dotnet build DLearning.slnx`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add src/Web.Api/Resources
git commit -m "feat: add en/vi resx keys for color names and examples"
```

---

## Task A5: `GET /colors` endpoint (JWT, localized)

**Files:**
- Create: `src/Web.Api/Endpoints/Colors/GetColors.cs`

**Interfaces:**
- Consumes: `IQueryHandler<GetColorsQuery, IReadOnlyList<ColorDto>>`, `IStringLocalizer<SharedResource>`.
- Produces: `GET /colors` (JWT) → `[{ code, name, hexValue, exampleWord, exampleEmoji, displayOrder }]`.

- [ ] **Step 1: Write the endpoint**

Create `src/Web.Api/Endpoints/Colors/GetColors.cs`:

```csharp
using Application.Abstractions.Messaging;
using Application.Colors;
using Application.Colors.Data;
using Microsoft.Extensions.Localization;
using SharedKernel;
using Web.Api.Infrastructure;
using Web.Api.Middleware;

namespace Web.Api.Endpoints.Colors;

internal sealed class GetColors : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/colors", async (
            IQueryHandler<GetColorsQuery, IReadOnlyList<ColorDto>> handler,
            IStringLocalizer<SharedResource> localizer,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<ColorDto>> result =
                await handler.Handle(new GetColorsQuery(), cancellationToken);

            return result.ToHttpResult(colors => Results.Ok(
                colors.Select(c => new
                {
                    c.Code,
                    name = localizer["Color." + c.Code].Value,
                    c.HexValue,
                    exampleWord = localizer["ColorExample." + c.Code].Value,
                    c.ExampleEmoji,
                    c.DisplayOrder
                })));
        })
        .RequireAuthorization();
    }
}
```

Note: `SharedResource` lives in the `Web.Api` root namespace, so it resolves without a `using` from `Web.Api.Endpoints.Colors` (ancestor-namespace lookup) — matching how the Catalog endpoints reference it.

- [ ] **Step 2: Build + manual smoke test**

Run: `dotnet build DLearning.slnx` → `Build succeeded.`

```bash
docker compose up -d
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Web.Api &
for i in $(seq 1 40); do curl -s -o /dev/null "http://localhost:5113/colors" && break; sleep 1; done
echo "no token (expect 401): $(curl -s -o /dev/null -w '%{http_code}' http://localhost:5113/colors)"
TOKEN=$(curl -s -X POST http://localhost:5113/users/login -H 'Content-Type: application/json' -d '{"identifier":"demo","password":"Demo@123"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
echo "en first:"; curl -s "http://localhost:5113/colors?lang=en" -H "Authorization: Bearer $TOKEN" | grep -o '"name":"[^"]*"' | head -1
echo "vi first:"; curl -s "http://localhost:5113/colors?lang=vi" -H "Authorization: Bearer $TOKEN" | grep -o '"name":"[^"]*"' | head -1
echo "count:"; curl -s "http://localhost:5113/colors?lang=vi" -H "Authorization: Bearer $TOKEN" | grep -o '"code"' | wc -l | tr -d ' '
pkill -f Web.Api; docker compose down
```
Expected: `401`; en first `"name":"Red"`; vi first `"name":"Đỏ"`; count `11`.

- [ ] **Step 3: Commit**

```bash
git add src/Web.Api/Endpoints/Colors
git commit -m "feat: add JWT-protected localized GET /colors endpoint"
```

---

## Task A6: Colors integration tests

**Files:**
- Create: `tests/Api.IntegrationTests/Colors/ColorsEndpointsTests.cs`

**Interfaces:**
- Consumes: `ApiTestFactory` (Testcontainers Postgres; migration seeds 11 colors + demo user).

- [ ] **Step 1: Write the integration tests**

Create `tests/Api.IntegrationTests/Colors/ColorsEndpointsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Api.IntegrationTests.Colors;

public sealed class ColorsEndpointsTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record TokenOnly(string Token);
    private sealed record ColorView(string Code, string Name, string HexValue, string ExampleWord, string ExampleEmoji, int DisplayOrder);
    private sealed record ProductView(string Code, string Name, string IconKey, int DisplayOrder, bool IsAvailable);

    private async Task<string> LoginAsync()
    {
        var login = await _client.PostAsJsonAsync("/users/login",
            new { identifier = "demo", password = "Demo@123" });
        var auth = await login.Content.ReadFromJsonAsync<TokenOnly>();
        auth.ShouldNotBeNull();
        return auth.Token;
    }

    [Fact]
    public async Task GetColors_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/colors");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetColors_En_Returns11LocalizedColorsInOrder()
    {
        var token = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/colors?lang=en");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var colors = await response.Content.ReadFromJsonAsync<List<ColorView>>();
        colors.ShouldNotBeNull();
        colors.Count.ShouldBe(11);
        colors[0].Code.ShouldBe("red");
        colors[0].Name.ShouldBe("Red");
        colors[0].ExampleWord.ShouldBe("apple");
        colors[0].HexValue.ShouldBe("#EF4444");
        colors[^1].Code.ShouldBe("gray");
    }

    [Fact]
    public async Task GetColors_Vi_ReturnsVietnameseNames()
    {
        var token = await LoginAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/colors?lang=vi");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        var colors = await response.Content.ReadFromJsonAsync<List<ColorView>>();
        colors.ShouldNotBeNull();
        colors[0].Name.ShouldBe("Đỏ");
        colors[0].ExampleWord.ShouldBe("quả táo");
    }

    [Fact]
    public async Task PreschoolProducts_ColorsIsNowAvailable()
    {
        var products = await _client.GetFromJsonAsync<List<ProductView>>("/categories/preschool/products?lang=en");

        products.ShouldNotBeNull();
        products.Single(p => p.Code == "colors").IsAvailable.ShouldBeTrue();
    }
}
```

- [ ] **Step 2: Run the full backend suite**

Run: `cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-be && dotnet test`
Expected: PASS — Architecture + Unit + Integration all green (integration needs Docker).

- [ ] **Step 3: Commit**

```bash
git add tests/Api.IntegrationTests/Colors
git commit -m "test: add integration tests for colors endpoint and product availability"
```

---

# Phase B — Frontend Colors screen (`dlearning-web`)

> Paths relative to `/Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-web`.

## Task B1: Color model, port, use case

**Files:**
- Create: `src/domain/models/Color.ts`
- Create: `src/domain/repositories/ColorRepository.ts`
- Create: `src/application/usecases/getColors.ts`
- Create: `src/application/usecases/getColors.test.ts`

**Interfaces:**
- Produces: `Color { code, name, hexValue, exampleWord, exampleEmoji, displayOrder }`; `ColorRepository { getColors(lang: Lang): Promise<Color[]> }`; `makeGetColors(repo)` factory.

- [ ] **Step 1: Write the failing test**

Create `src/application/usecases/getColors.test.ts`:

```ts
import { describe, it, expect, vi } from 'vitest';
import { makeGetColors } from './getColors';
import type { ColorRepository } from '../../domain/repositories/ColorRepository';

describe('getColors use case', () => {
  it('passes the language through to the repository', async () => {
    const colors = [{ code: 'red', name: 'Đỏ', hexValue: '#EF4444', exampleWord: 'quả táo', exampleEmoji: '🍎', displayOrder: 1 }];
    const repo: ColorRepository = { getColors: vi.fn().mockResolvedValue(colors) };

    const getColors = makeGetColors(repo);
    const result = await getColors('vi');

    expect(repo.getColors).toHaveBeenCalledWith('vi');
    expect(result).toEqual(colors);
  });
});
```

- [ ] **Step 2: Run to verify it fails**

Run: `npm test -- getColors`
Expected: FAIL (missing modules).

- [ ] **Step 3: Write the model, port, use case**

Create `src/domain/models/Color.ts`:

```ts
export interface Color {
  code: string;
  name: string;
  hexValue: string;
  exampleWord: string;
  exampleEmoji: string;
  displayOrder: number;
}
```

Create `src/domain/repositories/ColorRepository.ts`:

```ts
import type { Color } from '../models/Color';
import type { Lang } from './CatalogRepository';

export interface ColorRepository {
  getColors(lang: Lang): Promise<Color[]>;
}
```

Create `src/application/usecases/getColors.ts`:

```ts
import type { Color } from '../../domain/models/Color';
import type { ColorRepository } from '../../domain/repositories/ColorRepository';
import type { Lang } from '../../domain/repositories/CatalogRepository';

export function makeGetColors(repo: ColorRepository) {
  return (lang: Lang): Promise<Color[]> => repo.getColors(lang);
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `npm test -- getColors`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/dlearning-web
git add src/domain/models/Color.ts src/domain/repositories/ColorRepository.ts src/application/usecases/getColors.ts src/application/usecases/getColors.test.ts
git commit -m "feat: add color model, repository port, and use case"
```

---

## Task B2: ApiColorRepository + DI wiring

**Files:**
- Create: `src/infrastructure/repositories/ApiColorRepository.ts`
- Modify: `src/di/container.ts`

**Interfaces:**
- Consumes: `HttpClient.get(path, query)` (JWT bearer added automatically).
- Produces: `ApiColorRepository implements ColorRepository`; `container.getColors`.

- [ ] **Step 1: Write the API repository**

Create `src/infrastructure/repositories/ApiColorRepository.ts`:

```ts
import type { Color } from '../../domain/models/Color';
import type { ColorRepository } from '../../domain/repositories/ColorRepository';
import type { Lang } from '../../domain/repositories/CatalogRepository';
import type { HttpClient } from '../http/HttpClient';

export class ApiColorRepository implements ColorRepository {
  private readonly http: HttpClient;

  constructor(http: HttpClient) {
    this.http = http;
  }

  getColors(lang: Lang): Promise<Color[]> {
    return this.http.get<Color[]>('/colors', { lang });
  }
}
```

- [ ] **Step 2: Wire the container**

In `src/di/container.ts`, add imports:

```ts
import { makeGetColors } from '../application/usecases/getColors';
import { ApiColorRepository } from '../infrastructure/repositories/ApiColorRepository';
```

Inside `createContainer`, after `catalogRepository`:

```ts
  const colorRepository = new ApiColorRepository(http);
```

And in the returned object, add:

```ts
    getColors: makeGetColors(colorRepository),
```

- [ ] **Step 3: Type-check + tests**

Run: `npx tsc -b && npm test`
Expected: build clean; all tests green.

- [ ] **Step 4: Commit**

```bash
git add src/infrastructure/repositories/ApiColorRepository.ts src/di/container.ts
git commit -m "feat: add ApiColorRepository and wire getColors use case"
```

---

## Task B3: ColorCard + ColorDetail components

**Files:**
- Create: `src/presentation/components/ColorCard.tsx`
- Create: `src/presentation/components/ColorDetail.tsx`

**Interfaces:**
- Consumes: `Color`.
- Produces: `ColorCard({ color, onClick })`; `ColorDetail({ color, hasPrev, hasNext, onPrev, onNext, onClose })`.

- [ ] **Step 1: Write `ColorCard`**

Create `src/presentation/components/ColorCard.tsx`:

```tsx
import type { Color } from '../../domain/models/Color';

interface ColorCardProps {
  color: Color;
  onClick: () => void;
}

export function ColorCard({ color, onClick }: ColorCardProps) {
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
      <div
        style={{
          height: 96,
          background: color.hexValue,
          border: '1px solid rgba(0,0,0,.08)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: 40,
        }}
      >
        {color.exampleEmoji}
      </div>
      <div style={{ padding: '14px 16px 16px' }}>
        <div style={{ fontFamily: 'Sora', fontWeight: 700, fontSize: 15 }}>{color.name}</div>
        <div style={{ fontSize: 12, color: '#8A8A99', marginTop: 2 }}>{color.exampleWord}</div>
      </div>
    </button>
  );
}
```

- [ ] **Step 2: Write `ColorDetail` (modal)**

Create `src/presentation/components/ColorDetail.tsx`:

```tsx
import type { CSSProperties } from 'react';
import type { Color } from '../../domain/models/Color';

interface ColorDetailProps {
  color: Color;
  hasPrev: boolean;
  hasNext: boolean;
  onPrev: () => void;
  onNext: () => void;
  onClose: () => void;
}

export function ColorDetail({ color, hasPrev, hasNext, onPrev, onNext, onClose }: ColorDetailProps) {
  return (
    <div
      onClick={onClose}
      style={{ position: 'fixed', inset: 0, background: 'rgba(20,10,40,.45)', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 20, zIndex: 50 }}
    >
      <div onClick={(e) => e.stopPropagation()} style={{ width: '100%', maxWidth: 420, background: '#fff', borderRadius: 24, overflow: 'hidden', animation: 'fadeUp .3s ease both' }}>
        <div style={{ background: color.hexValue, borderBottom: '1px solid rgba(0,0,0,.08)', padding: 40, textAlign: 'center' }}>
          <div style={{ fontSize: 84, lineHeight: 1 }}>{color.exampleEmoji}</div>
        </div>
        <div style={{ padding: '22px 26px 26px', textAlign: 'center' }}>
          <div style={{ fontSize: 12, color: '#8A8A99', textTransform: 'uppercase', letterSpacing: '.06em' }}>{color.hexValue}</div>
          <div style={{ fontFamily: 'Sora', fontWeight: 700, fontSize: 26, margin: '4px 0 6px' }}>{color.name}</div>
          <div style={{ fontSize: 15, color: '#6B6B80', marginBottom: 18 }}>{color.exampleEmoji} {color.exampleWord}</div>
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

- [ ] **Step 3: Type-check**

Run: `npx tsc -b`
Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add src/presentation/components/ColorCard.tsx src/presentation/components/ColorDetail.tsx
git commit -m "feat: add ColorCard and ColorDetail components"
```

---

## Task B4: ColorsPage + route + home wiring

**Files:**
- Create: `src/presentation/pages/ColorsPage.tsx`
- Modify: `src/presentation/router.tsx`
- Modify: `src/presentation/pages/HomePage.tsx`

**Interfaces:**
- Consumes: `container.getColors`, `useI18n`, `useAuth`, `ColorCard`, `ColorDetail`, `LanguageSwitcher`.
- Produces: `ColorsPage` at `/colors` (guarded); generalized `HomePage.openProduct`.

- [ ] **Step 1: Write `ColorsPage`**

Create `src/presentation/pages/ColorsPage.tsx`:

```tsx
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { container } from '../../di/container';
import type { Color } from '../../domain/models/Color';
import { ColorCard } from '../components/ColorCard';
import { ColorDetail } from '../components/ColorDetail';
import { LanguageSwitcher } from '../components/LanguageSwitcher';
import { useI18n } from '../i18n/useI18n';

type Status = 'loading' | 'ready' | 'error';

export function ColorsPage() {
  const { lang, t } = useI18n();
  const navigate = useNavigate();
  const [colors, setColors] = useState<Color[]>([]);
  const [status, setStatus] = useState<Status>('loading');
  const [selected, setSelected] = useState<number | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    setStatus('loading');
    container.getColors(lang)
      .then((data) => { if (active) { setColors(data); setStatus('ready'); } })
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
            {colors.map((color, index) => (
              <ColorCard key={color.code} color={color} onClick={() => setSelected(index)} />
            ))}
          </div>
        )}
      </main>

      {selected !== null && colors[selected] && (
        <ColorDetail
          color={colors[selected]}
          hasPrev={selected > 0}
          hasNext={selected < colors.length - 1}
          onPrev={() => setSelected((s) => (s === null ? s : Math.max(0, s - 1)))}
          onNext={() => setSelected((s) => (s === null ? s : Math.min(colors.length - 1, s + 1)))}
          onClose={() => setSelected(null)}
        />
      )}
    </div>
  );
}
```

- [ ] **Step 2: Add the `/colors` route**

In `src/presentation/router.tsx`, add the import and a guarded route (place after the `/alphabet` route):

```tsx
import { ColorsPage } from './pages/ColorsPage';
```

```tsx
  {
    path: '/colors',
    element: (
      <RequireAuth>
        <ColorsPage />
      </RequireAuth>
    ),
  },
```

- [ ] **Step 3: Generalize `HomePage.openProduct`**

In `src/presentation/pages/HomePage.tsx`, replace the `openProduct` function so any available product opens its own route:

```tsx
  function openProduct(product: Product) {
    if (product.isAvailable) {
      navigate(`/${product.code}`);
    }
  }
```

(The card is already disabled for unavailable products; `alphabet` → `/alphabet` and `colors` → `/colors` both resolve to existing routes.)

- [ ] **Step 4: Build + tests**

Run: `npm run build && npm test`
Expected: `tsc -b` + `vite build` succeed; all Vitest tests green.

- [ ] **Step 5: Commit**

```bash
git add src/presentation/pages/ColorsPage.tsx src/presentation/router.tsx src/presentation/pages/HomePage.tsx
git commit -m "feat: add colors learning page, route, and generalized product navigation"
```

---

# Phase C — Deploy & verify

## Task C1: Rebuild the full stack and verify colors end-to-end

**Files:** none (deploy verification)

- [ ] **Step 1: Rebuild + restart the stack**

Run:
```bash
cd /Users/dat.nguyenmanh/Desktop/dat/my-git/dlearning/orchestrator/deploy
docker compose up --build -d
for i in $(seq 1 60); do curl -s -o /dev/null "http://localhost:8080/api/categories" && break; sleep 2; done
```
Expected: stack rebuilds (backend auto-applies the `Colors` migration + seed on start).

- [ ] **Step 2: Verify colors end-to-end through nginx**

Run:
```bash
TOKEN=$(curl -s -X POST "http://localhost:8080/api/users/login" -H 'Content-Type: application/json' -d '{"identifier":"demo@dlearning.vn","password":"Demo@123"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
echo "colors no-token (expect 401): $(curl -s -o /dev/null -w '%{http_code}' http://localhost:8080/api/colors)"
echo "colors count (expect 11): $(curl -s 'http://localhost:8080/api/colors?lang=en' -H "Authorization: Bearer $TOKEN" | grep -o '"code"' | wc -l | tr -d ' ')"
echo "colors vi first (expect Đỏ): $(curl -s 'http://localhost:8080/api/colors?lang=vi' -H "Authorization: Bearer $TOKEN" | grep -o '"name":"[^"]*"' | head -1)"
echo "product colors available (expect true): $(curl -s 'http://localhost:8080/api/categories/preschool/products?lang=en' | grep -o '"code":"colors","name":"[^"]*","iconKey":"[^"]*","displayOrder":[0-9]*,"isAvailable":[a-z]*' )"
```
Expected: `401`; `11`; `"name":"Đỏ"`; the products line ends `"isAvailable":true`.

- [ ] **Step 3: Manual browser check**

Open `http://localhost:8080`, log in (`demo@dlearning.vn` / `Demo@123`). On the preschool menu, the **Colors / Màu sắc** card is no longer "coming soon" — click it → the `/colors` screen shows 11 swatches; click a swatch → detail modal with the color, example emoji + word; toggle VI/EN → names switch. Then optionally `docker compose down`.

- [ ] **Step 4 (optional): Note**

No commit here (verification only). If the orchestrator later becomes a git repo, the compose is already committed there; nothing changed in this task.

---

# Self-Review (coverage map)

- **Spec §3.1 Colors slice** (Color entity structural-only) → A1; query/handler/DbSet → A2; EF config + seed + product flip + migration → A3.
- **Spec §3.1 Web.Api localization keys** → A4; **JWT localized endpoint** → A5.
- **Spec §3.2 API contract** (`GET /colors` JWT) → A5.
- **Spec §3.3 backend tests** (unit + integration, arch stays green) → A2, A6.
- **Spec §3.4 color data (11 colors + examples)** → A3 (structural seed) + A4 (localized names/examples).
- **Spec §4.1 FE clean-arch pieces** (model, port, use case, ApiColorRepository, DI) → B1, B2.
- **Spec §4.2 Colors screen** (`/colors`, ColorCard swatch grid + ColorDetail modal, header + switcher, openProduct generalized) → B3, B4.
- **Spec §4.3 FE tests** → B1.
- **Spec §5 deploy & run** (single-DB stack at :8080, migration on start) → C1.
