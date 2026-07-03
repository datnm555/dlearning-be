# dLearning — "Colors" (Màu sắc) Learning Feature — Design Spec

**Date:** 2026-07-03
**Repos:** `dlearning-be` (Colors slice + resx + migration), `dlearning-web` (Colors screen).
**Builds on:** the existing Alphabets slice (pattern to mirror), the Catalog/i18n system (resx localization), and the seeded `colors` product under `preschool`.

## 1. Goal

Turn the preschool **Colors** product from a "coming soon" menu item into a working learning screen: a JWT-protected `GET /colors` API returning 11 basic colors (localized names + example objects), and a `/colors` screen mirroring the alphabet screen (grid of swatches → detail modal). After this, tapping the Colors card in the home menu opens the lesson.

## 2. Approach

Colors mirror the **Alphabets** slice structurally, but — unlike alphabet letter names, which are inherently Vietnamese and stored in the DB — **color names and example words genuinely translate**, so they are localized via **`.resx`** (consistent with the Catalog). The database stores only language-neutral color data (`code`, `hexValue`, `exampleEmoji`, `displayOrder`).

## 3. Backend (`dlearning-be`)

### 3.1 `Colors` slice

**Domain (`src/Domain/Colors/`):**
- `Color : Entity` (reference data, mirrors `AlphabetLetter`) — `init` props: `Code` (string, unique), `HexValue` (string, e.g. `#EF4444`), `ExampleEmoji` (string), `DisplayOrder` (int). No name/exampleWord in the DB (resx).

**Application (`src/Application/Colors/`):**
- `GetColorsQuery() → Result<IReadOnlyList<ColorDto>>`, ordered by `DisplayOrder`.
  `ColorDto(string Code, string HexValue, string ExampleEmoji, int DisplayOrder)`.
- `internal sealed` handler reading `IApplicationDbContext.Colors`. Add `DbSet<Color> Colors` to the port + `ApplicationDbContext`. Register the handler in `Application/DependencyInjection.cs`.
- No `Microsoft.AspNetCore`/localization dependency in Application (localization is a Web.Api concern).

**Infrastructure (`src/Infrastructure/Colors/`):**
- `ColorConfiguration : IEntityTypeConfiguration<Color>` — table `colors`; `Code` unique index; `builder.Ignore(c => c.DomainEvents)`; `HasData` seed (11 rows, static GUIDs `c0100000-0000-4000-8000-0000000000NN`, NN = order 01..0b).
- **Product availability**: change the `colors` row in `ProductConfiguration`'s `HasData` seed to `IsAvailable = true`. EF will emit an `UpdateData` in the migration for that existing seeded row.
- One EF migration `Colors` (creates `colors` table + seed, and updates the product row). Run `dotnet ef` with `ASPNETCORE_ENVIRONMENT=Production`, output to `Database/Migrations`.

**Web.Api:**
- `Endpoints/Colors/GetColors.cs` — `GET /colors`, **`RequireAuthorization()`** (JWT, like `/alphabet`). Injects the query handler + `IStringLocalizer<SharedResource>`; maps each `ColorDto` → `{ code, name = localizer["Color." + code], hexValue, exampleWord = localizer["ColorExample." + code], exampleEmoji, displayOrder }` (use `.Value` on the localized strings).
- Add resx keys to `Resources/SharedResource.en.resx` and `SharedResource.vi.resx`: `Color.<code>` (color name) and `ColorExample.<code>` (example word) for all 11 codes.

### 3.2 Color data (11 colors)

| # | code | vi name | en name | hex | example emoji | vi example | en example |
|---|------|---------|---------|-----|---------------|------------|------------|
| 1 | red | Đỏ | Red | #EF4444 | 🍎 | quả táo | apple |
| 2 | orange | Cam | Orange | #F97316 | 🍊 | quả cam | orange |
| 3 | yellow | Vàng | Yellow | #FACC15 | 🍌 | quả chuối | banana |
| 4 | green | Xanh lá | Green | #22C55E | 🍃 | lá cây | leaf |
| 5 | blue | Xanh dương | Blue | #3B82F6 | 🫐 | quả việt quất | blueberry |
| 6 | purple | Tím | Purple | #A855F7 | 🍇 | quả nho | grapes |
| 7 | pink | Hồng | Pink | #EC4899 | 🌸 | bông hoa | flower |
| 8 | brown | Nâu | Brown | #92573B | 🍫 | sô cô la | chocolate |
| 9 | black | Đen | Black | #1F2937 | 🐈‍⬛ | mèo đen | black cat |
| 10 | white | Trắng | White | #F9FAFB | ☁️ | đám mây | cloud |
| 11 | gray | Xám | Gray | #9CA3AF | 🐘 | con voi | elephant |

### 3.3 API contract

| Method | Route | Auth | Response |
|--------|-------|------|----------|
| GET | `/colors?lang=vi` | **JWT** | `[{ code, name, hexValue, exampleWord, exampleEmoji, displayOrder }]` ordered; `401` without a valid token |

`lang` optional → default `vi` (or `Accept-Language`), via the existing request-localization middleware.

### 3.4 Backend tests

- **Application.UnitTests**: `GetColorsQueryHandlerTests` — returns colors ordered by `DisplayOrder` (MockQueryable, build the mock DbSet into a local var first).
- **Api.IntegrationTests**: `ColorsEndpointsTests` — `GET /colors` without token → `401`; with the demo token → 11 items, `?lang=en` first item `name == "Red"` and `exampleWord == "apple"`, `?lang=vi` → `"Đỏ"`/`"quả táo"`, colors ordered (`red` first, `gray` last). Also assert `GET /categories/preschool/products` now shows `colors.isAvailable == true`.
- **ArchitectureTests**: unchanged, must stay green.

## 4. Frontend (`dlearning-web`)

### 4.1 New clean-arch pieces

- `domain/models/Color.ts` `{ code, name, hexValue, exampleWord, exampleEmoji, displayOrder }`.
- `domain/repositories/ColorRepository.ts` — `getColors(lang: Lang): Promise<Color[]>`.
- `application/usecases/getColors.ts` — `makeGetColors(repo)` factory (+ `getColors.test.ts`).
- `infrastructure/repositories/ApiColorRepository.ts` — `this.http.get<Color[]>('/colors', { lang })` (JWT bearer added automatically by `HttpClient`).
- `di/container.ts` — wire `ApiColorRepository` + `getColors`.

### 4.2 Colors screen

- `presentation/pages/ColorsPage.tsx` at `/colors` (guarded by `RequireAuth`), mirroring `AlphabetPage`:
  - Header: back link → `/`, app title, `LanguageSwitcher`.
  - On mount (and when `lang` changes) fetch `container.getColors(lang)`; loading / error+retry / ready states like AlphabetPage.
  - Grid of `ColorCard` (new component): the card's top block is the **color swatch** filled with `hexValue`, with a subtle `1px solid rgba(0,0,0,.08)` border so white/light swatches stay visible; shows `exampleEmoji` overlaid or below, and the localized `name`. Click → open detail.
  - `ColorDetail.tsx` modal (new component, mirrors `LetterDetail`): large swatch, color `name`, `exampleWord` + `exampleEmoji`, prev/next navigation, close.
- `presentation/router.tsx`: add guarded `/colors` route.
- `presentation/pages/HomePage.tsx`: generalize `openProduct` — for an available product, `navigate('/' + product.code)` (so `alphabet` → `/alphabet`, `colors` → `/colors`). The Colors card stops showing "coming soon" because the API now returns `isAvailable = true`.

### 4.3 Frontend tests

- Vitest: `getColors.test.ts` — passes `lang` through to the repository and returns the colors.

## 5. Deploy & run

Rebuild the single-DB full stack:
```bash
cd orchestrator/deploy && docker compose up --build -d      # → http://localhost:8080
```
The backend auto-applies the `Colors` migration on start (creates the table + seed, marks the product available). Open `:8080`, log in (`demo@dlearning.vn` / `Demo@123`), and the preschool menu's **Colors** card opens the working lesson; the language switcher flips names between vi/en.

**Done when:** `GET /colors` returns 11 localized colors (401 without a token); the Colors card opens `/colors` with swatches + detail modal; the language switcher localizes color names; the full stack shows it at `:8080`; all backend + frontend tests pass.

## 6. Out of scope

- Audio pronunciation, quizzes/games/matching.
- Animals & counting products (remain "coming soon").
- Admin CRUD; colors beyond the 11 basic set.
