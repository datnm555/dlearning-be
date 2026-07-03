# dLearning — Catalog (Category/Product) + i18n + Docker — Design Spec

**Date:** 2026-07-03
**Repos:** `dlearning-be` (API + i18n resources + Dockerfile), `dlearning-web` (menu UI + i18n + Dockerfile), `orchestrator` (full-stack docker-compose).
**Builds on:** the existing Users (auth/JWT) and Alphabets slices.

## 1. Goals

- Add an education-level **taxonomy**: a `Category` list (Preschool, Primary, Secondary, High School, University) and, under Preschool, a `Product` menu (Alphabet, Animals, Colors, Counting 1–10).
- Serve category/product **display names in English and Vietnamese**, chosen per request.
- The frontend lets the user **pick a language** (persisted) and loads the menu from the API.
- **Dockerize** both apps and run the whole system (Postgres + API + web) with one `docker compose up`.

Products are **menu items** this round. Only **Alphabet** links to a working screen (the existing `/alphabet`); Animals/Colors/Counting render a "coming soon" state. No learning content is seeded for those three.

## 2. i18n strategy (decided)

Two kinds of text, two mechanisms — standard practice:

- **Dynamic domain data** (category/product **display names**): the taxonomy is fixed and developer-owned, so names live in **`.resx` resource files** on the backend, keyed by the entity's stable `Code`. The **database stores only structural data** (`Code`, `IconKey`, `DisplayOrder`, relationships) — never display names.
- **Static UI chrome** (buttons, page titles, the language switcher, generic labels): a **JSON i18n catalog** on the frontend (`src/i18n/{en,vi}.json`).

Supported cultures: `en`, `vi`. **Default: `vi`.** The API resolves culture from `?lang=` first, then the `Accept-Language` header.

Tradeoff accepted: adding a category/product later requires a `.resx` key **and** a DB row (not just a DB insert). Fine for a fixed taxonomy.

## 3. Backend (`dlearning-be`)

### 3.1 New `Catalog` vertical slice

**Domain (`src/Domain/Catalog/`):**
- `Category : AggregateRoot` — `Code` (string, unique), `IconKey` (string), `DisplayOrder` (int). No name (resx). Reference data; no behavior beyond a `Create` factory used by seeding.
- `Product : Entity` — `CategoryId` (Guid FK), `Code` (string), `IconKey` (string), `DisplayOrder` (int), `IsAvailable` (bool). `Code` unique within a category.
- No domain events. Both are reference data (mirrors `AlphabetLetter`'s shape).

**Application (`src/Application/Catalog/`):**
- `GetCategoriesQuery() → Result<IReadOnlyList<CategoryDto>>`, ordered by `DisplayOrder`.
  `CategoryDto(string Code, string IconKey, int DisplayOrder)` — **no name; localization is a Web.Api concern.**
- `GetProductsByCategoryQuery(string CategoryCode) → Result<IReadOnlyList<ProductDto>>`, filtered by the category's `Code`, ordered by `DisplayOrder`.
  `ProductDto(string Code, string IconKey, int DisplayOrder, bool IsAvailable)`.
  Unknown category code → empty list (200 with `[]`), not an error.
- Handlers are `internal sealed` and read from `IApplicationDbContext` (add `DbSet<Category> Categories`, `DbSet<Product> Products`).
- **No `Microsoft.AspNetCore` / localization dependency in Application** — keeps the architecture test green.

**Infrastructure:**
- `CategoryConfiguration`, `ProductConfiguration` (`IEntityTypeConfiguration<>`), table names `categories`, `products`; unique index on `Category.Code` and on `(Product.CategoryId, Product.Code)`; `Product` → `Category` FK. `builder.Ignore(x => x.DomainEvents)`.
- `HasData` seed with static GUIDs:
  - Categories (order 1–5): `preschool`, `primary`, `secondary`, `highschool`, `university`.
  - Products under `preschool` (order 1–4): `alphabet` (IsAvailable=true), `animals`, `colors`, `counting` (IsAvailable=false).
  - `IconKey` values are emoji or icon slugs (e.g. `preschool`→`🧸`, `alphabet`→`🔤`, `animals`→`🐾`, `colors`→`🎨`, `counting`→`🔢`) — FE decides rendering.
- One new EF migration `Catalog` (schema + seed), added into `src/Infrastructure/Database/Migrations/` (run EF with `ASPNETCORE_ENVIRONMENT=Production` as before).

**Web.Api — localization + endpoints:**
- Add packages: none beyond the framework (`Microsoft.Extensions.Localization` ships with ASP.NET Core shared framework).
- `Program.cs`: `builder.Services.AddLocalization(o => o.ResourcesPath = "Resources")`; configure `RequestLocalizationOptions` with supported cultures `["en","vi"]`, default `vi`, and provider order `[QueryStringRequestCultureProvider (key "lang"), AcceptLanguageHeaderRequestCultureProvider]`; `app.UseRequestLocalization(...)` before `MapEndpoints`.
- `Resources/SharedResource.cs` (empty marker class) + `Resources/SharedResource.en.resx` / `Resources/SharedResource.vi.resx` with keys:
  | Key | en | vi |
  |-----|----|----|
  | `Category.preschool` | Preschool | Mầm non |
  | `Category.primary` | Primary School | Cấp 1 |
  | `Category.secondary` | Secondary School | Cấp 2 |
  | `Category.highschool` | High School | Cấp 3 |
  | `Category.university` | University | Đại học |
  | `Product.alphabet` | Alphabet | Bảng chữ cái |
  | `Product.animals` | Animals | Con vật |
  | `Product.colors` | Colors | Màu sắc |
  | `Product.counting` | Counting 1–10 | Học đếm 1–10 |
- `Endpoints/Catalog/GetCategories.cs` (anonymous): injects the query handler + `IStringLocalizer<SharedResource>`; maps each `CategoryDto` → `{ code, name = localizer["Category." + code], iconKey, displayOrder }`.
- `Endpoints/Catalog/GetProductsByCategory.cs` (anonymous): route `GET /categories/{code}/products`; maps `ProductDto` → `{ code, name = localizer["Product." + code], iconKey, displayOrder, isAvailable }`.
- Missing resx key → `IStringLocalizer` returns the key string (graceful, no crash).

### 3.2 API contract

| Method | Route | Auth | Response |
|--------|-------|------|----------|
| GET | `/categories?lang=vi` | anon | `[{ code, name, iconKey, displayOrder }]` ordered |
| GET | `/categories/{code}/products?lang=vi` | anon | `[{ code, name, iconKey, displayOrder, isAvailable }]` ordered |

`lang` is optional; omitted → default `vi` (or `Accept-Language`). `/users/*` and `/alphabet` are unchanged.

### 3.3 Backend tests

- **Application.UnitTests**: `GetCategoriesQueryHandlerTests` (ordered by DisplayOrder), `GetProductsByCategoryQueryHandlerTests` (filters by category code, ordered, `isAvailable` preserved, unknown code → empty).
- **Api.IntegrationTests**: `GET /categories?lang=en` → English names incl. "Preschool"; `?lang=vi` → "Mầm non"; default (no lang) → Vietnamese; `GET /categories/preschool/products` → 4 items, `alphabet.isAvailable == true`, others `false`; `GET /categories/university/products` → `[]`.
- **ArchitectureTests**: unchanged and must stay green (Application still has no ASP.NET/localization dependency).

## 4. Frontend (`dlearning-web`)

### 4.1 i18n

- `src/i18n/en.json`, `src/i18n/vi.json` — static UI strings (app title, "Choose a lesson", "Coming soon", language names, logout, greeting template, etc.).
- `src/presentation/i18n/LanguageProvider.tsx` + `useI18n()` hook exposing `{ lang, setLang, t(key) }`. `lang` persisted in `localStorage` key `dlearning.lang`, default `vi`.
- `src/presentation/components/LanguageSwitcher.tsx` — VI/EN toggle in the header (and on the auth pages).
- The chosen `lang` is passed to catalog API calls as `?lang=`.

### 4.2 New clean-arch pieces

- `domain/models/Category.ts` `{ code, name, iconKey, displayOrder }`, `domain/models/Product.ts` `{ code, name, iconKey, displayOrder, isAvailable }`.
- `domain/repositories/CatalogRepository.ts` — `getCategories(lang): Promise<Category[]>`, `getProducts(categoryCode, lang): Promise<Product[]>`.
- `application/usecases/getCategories.ts`, `getProducts.ts` — factory fns `make<X>(repo)`.
- `infrastructure/repositories/ApiCatalogRepository.ts` — calls `/categories?lang=` and `/categories/{code}/products?lang=` via `HttpClient` (extend `HttpClient.get` to accept query params, or build the path with the lang).
- `di/container.ts` — wire `ApiCatalogRepository` + the two use cases.

### 4.3 Menu / Home page

- New `presentation/pages/HomePage.tsx` mounted at `/` (guarded by `RequireAuth`). Router `*` now redirects to `/` (was `/alphabet`).
- Behavior: on mount fetch categories (menu, rendered as tabs/pills); default-select `preschool`; fetch that category's products; render product cards (reuse the colourful card style). Re-fetch when `lang` changes.
- Product card click: `isAvailable && code === 'alphabet'` → navigate `/alphabet`; otherwise show a "coming soon" state (disabled card + `t('common.comingSoon')` badge).
- Header shows greeting (`session.user.displayName`), `LanguageSwitcher`, and logout.
- Loading / error / retry states mirror the existing AlphabetPage.

### 4.4 Frontend tests

- Vitest: use-case tests for `getCategories`/`getProducts` with a fake `CatalogRepository`; a `useI18n`/`LanguageProvider` test asserting `t()` returns the right string per `lang` and that `setLang` persists to `localStorage`.

## 5. Docker (full-stack)

### 5.1 Backend image (`dlearning-be/Dockerfile`)

- Multi-stage: `mcr.microsoft.com/dotnet/sdk:10.0` restore+publish → `mcr.microsoft.com/dotnet/aspnet:10.0` runtime. Expose `8080`.
- Configured via env: `ConnectionStrings__Database=Host=postgres;Port=5432;Database=dlearning;Username=postgres;Password=postgres`, `Jwt__Secret/Issuer/Audience/ExpirationInMinutes`, `ASPNETCORE_ENVIRONMENT=Development` (so `ApplyMigrations()` runs → schema + seed on start), `ASPNETCORE_URLS=http://+:8080`.
- `.dockerignore` for `bin/`, `obj/`, `.git`.

### 5.2 Frontend image (`dlearning-web/Dockerfile`)

- Multi-stage: `node:22` `npm ci && npm run build` → `nginx:alpine` serving `dist/`.
- `dlearning-web/nginx.conf`: SPA fallback `try_files $uri /index.html`; `location /api/ { proxy_pass http://backend:8080/; }` (strips `/api` so `/api/categories` → `backend:8080/categories`). Same `/api` contract as the Vite dev proxy, so no app code changes and no CORS.
- `.dockerignore` for `node_modules/`, `dist/`.

### 5.3 Full-stack compose (`orchestrator/deploy/docker-compose.yml`)

- Services:
  - `postgres` (`postgres:17-alpine`, db/user/pass `dlearning`/`postgres`/`postgres`, healthcheck, named volume).
  - `backend` — `build: ../../dlearning-be`, env as §5.1, `depends_on: postgres (healthy)`, expose 8080.
  - `frontend` — `build: ../../dlearning-web`, `depends_on: backend`, `ports: "8080:80"` (host 8080 → nginx). Open `http://localhost:8080`.
- Build contexts point at the sibling repos (`orchestrator/deploy/` → `../../dlearning-be`). Requires the repos present as siblings of the orchestrator (they are).
- The BE repo keeps its existing postgres-only `docker-compose.yml` for local `dotnet run` dev. This full-stack compose is separate and additive.

## 6. Run it

```bash
# Full stack (production-style)
cd orchestrator/deploy && docker compose up --build      # → http://localhost:8080

# Local dev (unchanged)
cd dlearning-be && docker compose up -d && dotnet run --project src/Web.Api
cd dlearning-web && npm run dev
```

**Done when:** the API returns localized categories/products for `?lang=en|vi`; the web menu lists categories + preschool products with a working language switcher; Alphabet opens the existing screen and the other three show "coming soon"; `docker compose up --build` in `orchestrator/deploy` serves the whole app at one URL; all backend + frontend tests pass.

## 7. Out of scope

- Learning content/screens for animals, colors, counting.
- Admin CRUD for categories/products (would flip i18n to DB-stored translations).
- Languages beyond en/vi; server-side per-user language preference.
- Products under primary/secondary/highschool/university (categories exist but stay empty).
