# dLearning — Animals (Con vật) + Counting (Học đếm 1–10) — Design Spec

**Date:** 2026-07-03
**Repos:** `dlearning-be` (Animals + Counting slices + resx + migration), `dlearning-web` (two screens).
**Builds on:** the Colors feature (exact pattern to mirror) and the Catalog/i18n system.

## 1. Goal

Turn the last two preschool "coming soon" products into working lessons: a JWT `GET /animals` (12 animals with localized name + sound) and `GET /counting` (numbers 1–10 with localized word + a visual quantity), each with its own learning screen. After this, all four preschool cards (Alphabet, Animals, Colors, Counting) are live.

## 2. Approach

Two independent slices, each a structural copy of the `Colors` slice: reference-data entity (language-neutral columns only) + query + EF seed; display strings (`name`, `sound`, `word`) localized via `.resx` in the Web.Api endpoint. Both products flip to available. Frontend adds two screens mirroring `ColorsPage`. The home menu already routes available products to `/<code>`, so no home-page change is needed.

## 3. Backend (`dlearning-be`)

### 3.1 Animals slice

**Domain (`src/Domain/Animals/`):** `Animal : Entity` — `init` props `Code` (unique), `Emoji`, `DisplayOrder`. No name/sound in DB (resx).

**Application (`src/Application/Animals/`):** `GetAnimalsQuery() → Result<IReadOnlyList<AnimalDto>>` ordered by `DisplayOrder`; `AnimalDto(string Code, string Emoji, int DisplayOrder)`. Add `DbSet<Animal> Animals` to the port + context; register handler.

**Infrastructure (`src/Infrastructure/Animals/`):** `AnimalConfiguration` — table `animals`, `Code` unique index, `Ignore(DomainEvents)`, `HasData` 12 rows, static GUIDs `a11a0000-0000-4000-8000-0000000000NN` (NN = order 01..0c).

**Web.Api:** `Endpoints/Animals/GetAnimals.cs` — `GET /animals`, `RequireAuthorization()`; injects handler + `IStringLocalizer<SharedResource>`; maps each `AnimalDto` → `{ code, name = localizer["Animal." + code], emoji, sound = localizer["AnimalSound." + code], displayOrder }`. resx keys `Animal.<code>` + `AnimalSound.<code>`.

**12 animals** (code · vi name · en name · emoji · vi sound · en sound):

| # | code | vi | en | emoji | vi sound | en sound |
|---|------|----|----|-------|----------|----------|
| 1 | cat | Mèo | Cat | 🐱 | meo meo | meow |
| 2 | dog | Chó | Dog | 🐶 | gâu gâu | woof |
| 3 | cow | Bò | Cow | 🐄 | ò ọ | moo |
| 4 | chicken | Gà | Chicken | 🐔 | cục tác | cluck |
| 5 | duck | Vịt | Duck | 🦆 | cạp cạp | quack |
| 6 | pig | Lợn | Pig | 🐷 | ụt ịt | oink |
| 7 | elephant | Voi | Elephant | 🐘 | ù ù | toot |
| 8 | lion | Sư tử | Lion | 🦁 | gừ gừ | roar |
| 9 | monkey | Khỉ | Monkey | 🐵 | khẹc khẹc | ooh ooh |
| 10 | bird | Chim | Bird | 🐦 | chíp chíp | tweet |
| 11 | fish | Cá | Fish | 🐟 | ục ục | blub |
| 12 | frog | Ếch | Frog | 🐸 | ộp ộp | ribbit |

### 3.2 Counting slice

**Domain (`src/Domain/Counting/`):** `CountingNumber : Entity` — `init` props `Value` (int, 1–10, unique), `Emoji`. Ordered by `Value` (no separate DisplayOrder).

**Application (`src/Application/Counting/`):** `GetCountingQuery() → Result<IReadOnlyList<CountingNumberDto>>` ordered by `Value`; `CountingNumberDto(int Value, string Emoji)`. Add `DbSet<CountingNumber> CountingNumbers` to the port + context; register handler.

**Infrastructure (`src/Infrastructure/Counting/`):** `CountingNumberConfiguration` — table `counting_numbers`, `Value` unique index, `Ignore(DomainEvents)`, `HasData` 10 rows, static GUIDs `c0117000-0000-4000-8000-0000000000NN` (NN = value 01..0a).

**Web.Api:** `Endpoints/Counting/GetCounting.cs` — `GET /counting`, `RequireAuthorization()`; maps each `CountingNumberDto` → `{ value, word = localizer["Number." + value], emoji }`. resx keys `Number.<value>` (1..10).

**10 numbers** (value · vi word · en word · emoji): 1·Một·One·🍎, 2·Hai·Two·🍌, 3·Ba·Three·🐟, 4·Bốn·Four·🌸, 5·Năm·Five·⭐, 6·Sáu·Six·🍇, 7·Bảy·Seven·🐞, 8·Tám·Eight·🐙, 9·Chín·Nine·🎈, 10·Mười·Ten·🍭. The UI shows the numeral + that emoji repeated `value` times + the word.

### 3.3 Migration + product flips

- In `ProductConfiguration`'s seed, change `animals` and `counting` tuples to `IsAvailable = true` (both were `false`).
- One EF migration `AnimalsAndCounting`: creates `animals` + `counting_numbers` tables (with seeds) and emits two `UpdateData` ops flipping the products. Run `dotnet ef` with `ASPNETCORE_ENVIRONMENT=Production`, output `Database/Migrations`.

### 3.4 API contract

| Method | Route | Auth | Response |
|--------|-------|------|----------|
| GET | `/animals?lang=vi` | JWT | `[{ code, name, emoji, sound, displayOrder }]` ordered |
| GET | `/counting?lang=vi` | JWT | `[{ value, word, emoji }]` ordered by value |

`lang` optional → default `vi` (existing request-localization).

### 3.5 Backend tests

- **Unit**: `GetAnimalsQueryHandlerTests` (ordered), `GetCountingQueryHandlerTests` (ordered by value). MockQueryable, build mock DbSet into a local var first.
- **Integration**: `AnimalsEndpointsTests` — `/animals` 401 without token; with token 12 items, en first `Cat`/`meow`, vi first `Mèo`/`meo meo`. `CountingEndpointsTests` — `/counting` 401; 10 items, first `value==1` en `One` vi `Một`, last `value==10`. Update the catalog products-availability test: all 4 preschool products now `IsAvailable=true`.
- **ArchitectureTests**: unchanged, stay green.

## 4. Frontend (`dlearning-web`)

### 4.1 Animals

- `domain/models/Animal.ts` `{ code, name, emoji, sound, displayOrder }`; `domain/repositories/AnimalRepository.ts` `getAnimals(lang)`; `application/usecases/getAnimals.ts` (+ test); `infrastructure/repositories/ApiAnimalRepository.ts` (`/animals?lang=`); wire in `container`.
- `presentation/components/AnimalCard.tsx` (big emoji, name, a `🔊 sound` line); `presentation/components/AnimalDetail.tsx` (big emoji, name, sound, prev/next, close); `presentation/pages/AnimalsPage.tsx` at `/animals` mirroring `ColorsPage`.

### 4.2 Counting

- `domain/models/CountingNumber.ts` `{ value, word, emoji }`; `domain/repositories/CountingRepository.ts` `getCounting(lang)`; `application/usecases/getCounting.ts` (+ test); `infrastructure/repositories/ApiCountingRepository.ts` (`/counting?lang=`); wire in `container`.
- `presentation/components/NumberCard.tsx` (numeral + `emoji` repeated `value` times + word); `presentation/components/NumberDetail.tsx` (big numeral, N emojis, word, prev/next, close); `presentation/pages/CountingPage.tsx` at `/counting`.

### 4.3 Router

Add guarded routes `/animals` → `AnimalsPage` and `/counting` → `CountingPage`. **No HomePage change** — `openProduct` already does `navigate('/' + product.code)` for available products.

### 4.4 Frontend tests

Vitest: `getAnimals.test.ts` and `getCounting.test.ts` — pass `lang` through to the repository and return the data.

## 5. Deploy & run

Rebuild the single-DB stack:
```bash
cd orchestrator/deploy && docker compose up --build -d      # → http://localhost:8080
```
Backend auto-applies the `AnimalsAndCounting` migration (2 tables + seeds + 2 product flips) on start. Open `:8080`, log in (`demo@dlearning.vn` / `Demo@123`) — the preschool menu now has all four cards live: Animals opens `/animals` (12 animals + sounds), Counting opens `/counting` (1–10 with N emojis); the language switcher localizes names/sounds/words.

**Done when:** `GET /animals` (12) and `GET /counting` (10) return localized data (401 without a token); both screens work; all four preschool products are available; the full stack shows it at `:8080`; all backend + frontend tests pass.

## 6. Out of scope

- Audio playback of animal sounds; quizzes/games/matching.
- Products/categories beyond preschool.
