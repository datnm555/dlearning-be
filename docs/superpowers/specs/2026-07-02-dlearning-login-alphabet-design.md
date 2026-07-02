# dLearning — Login & Bảng chữ cái tiếng Việt — Design Spec

**Ngày:** 2026-07-02
**Phạm vi:** Backend `dlearning-be` (feature Login + Alphabet API) và Frontend `dlearning-web` (trang Login theo design + màn học bảng chữ cái).

## 1. Mục tiêu

dLearning là nền tảng học trực tuyến; sản phẩm đầu tiên hướng tới học sinh mầm non học bảng chữ cái tiếng Việt.

- **Feature 1 — Login:** đăng ký + đăng nhập bằng email hoặc username, trả JWT.
- **Feature 2 — Bảng chữ cái:** API trả 29 chữ cái tiếng Việt chuẩn (kèm tên chữ, cách đọc, từ ví dụ) cho màn học của bé; yêu cầu đã đăng nhập.
- PostgreSQL chạy Docker; toàn bộ backend theo clean architecture của template `clean-architect-template`.

## 2. Cấu trúc tổng thể

```
dlearning/                      # thư mục cha (KHÔNG phải git repo)
├── dlearning-be/               # git repo — .NET 10 Web API
│   ├── docker-compose.yml      # PostgreSQL 17 (đặt trong repo BE để được version control)
│   ├── src/{SharedKernel,Domain,Application,Infrastructure,Web.Api}
│   └── tests/{ArchitectureTests,Application.UnitTests,Api.IntegrationTests}
└── dlearning-web/              # git repo — React + TypeScript (Vite)
    └── src/{domain,application,infrastructure,presentation,di}
```

## 3. Backend `dlearning-be`

### 3.1 Nền tảng

Sao chép khung từ `clean-architect-template` (.NET 10, EF Core 10 + Npgsql, Central Package Management, TreatWarningsAsErrors):

- Đổi namespace/solution `CleanArchitect` → `DLearning`.
- **Bỏ** slice mẫu `Examples/` (cả 4 layer + tests của nó).
- **Giữ nguyên:** SharedKernel (Result/Error/Entity/AggregateRoot/AuditedEntity…), ValidationDecorator + LoggingDecorator (thứ tự inner-to-outer: Validation trước, Logging sau), domain events dispatch-after-commit, AuditingInterceptor, 3 project test, `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`.
- Dependency rule `SharedKernel → Domain → Application → Infrastructure → Web.Api` tiếp tục được ArchitectureTests enforce.

Package thêm mới (vào `Directory.Packages.props`): `BCrypt.Net-Next` (hash mật khẩu), `Microsoft.AspNetCore.Authentication.JwtBearer` (JWT).

### 3.2 Slice `Users` (feature Login)

**Domain (`src/Domain/Users/`):**
- Aggregate `User : AggregateRoot` — thuộc tính: `Id (Guid)`, `Email` (unique, lưu lowercase), `Username` (unique, lowercase), `DisplayName`, `PasswordHash`. Kế thừa audit `CreatedAt/ModifiedAt` theo cơ chế template.
- Factory `User.Create(email, username, displayName, passwordHash)` trả `Result<User>`, raise `UserRegisteredDomainEvent`.
- `UserErrors`: `EmailNotUnique` (Conflict), `UsernameNotUnique` (Conflict), `InvalidCredentials` (Unauthorized → 401), `NotFound`.

**Application (`src/Application/Users/`):**
- `RegisterUserCommand(Email, Username, DisplayName, Password)` → `Result<Guid>`.
  - Validator: email đúng định dạng; username 3–30 ký tự chữ/số; displayName 1–100; password ≥ 8 ký tự.
  - Handler: kiểm tra email/username chưa tồn tại → hash password qua `IPasswordHasher` → tạo `User` → save.
- `LoginCommand(Identifier, Password)` → `Result<LoginResponse>`.
  - `Identifier` khớp **email hoặc username** (không phân biệt hoa thường). Số điện thoại trong design bỏ qua (YAGNI).
  - Sai identifier hay sai password đều trả cùng lỗi `InvalidCredentials` (không tiết lộ tài khoản tồn tại hay không).
  - `LoginResponse { Token, UserId, Email, Username, DisplayName }`.
- Ports mới trong `Application/Abstractions/Authentication/`: `IPasswordHasher { Hash, Verify }`, `ITokenProvider { Create(User) }`.

**Infrastructure:**
- `BCryptPasswordHasher`, `JwtTokenProvider` (HS256, claims: `sub`, `email`, `username`, `name`; hạn 60 phút; cấu hình `Jwt:Secret/Issuer/Audience/ExpirationInMinutes` trong appsettings — secret dev để trong `appsettings.Development.json`).
- `UserConfiguration : IEntityTypeConfiguration<User>` — unique index cho `Email` và `Username`.
- Seed tài khoản demo trong migration: `demo@dlearning.vn` / username `demo` / mật khẩu `Demo@123` (hash BCrypt tĩnh).

**Web.Api (`Endpoints/Users/`):**
- `POST /users/register` → 200 `{ id }` | 400 validation | 409 trùng email/username.
- `POST /users/login` → 200 `LoginResponse` | 401 `InvalidCredentials`.
- Cả hai endpoint là anonymous. Bật `AddAuthentication().AddJwtBearer()` + `AddAuthorization()`.
- Lưu ý: `ErrorType` của template chưa có loại map ra 401 — thêm `ErrorType.Unauthorized` vào SharedKernel và map → `StatusCodes.Status401Unauthorized` trong `ResultExtensions`.

### 3.3 Slice `Alphabets` (feature Bảng chữ cái)

**Domain (`src/Domain/Alphabets/`):**
- Entity `AlphabetLetter : Entity` (dữ liệu tham chiếu, không có hành vi nghiệp vụ): `Id (Guid)`, `UpperCase`, `LowerCase`, `Name` (tên chữ, vd "bê"), `Sound` (cách đọc, vd "bờ"), `ExampleWord`, `ExampleEmoji`, `DisplayOrder`.
- Không có domain event; màu sắc thẻ do FE tự gán theo `DisplayOrder` (màu là presentation, không phải domain data).

**Application (`src/Application/Alphabets/`):**
- `GetAlphabetQuery()` → `Result<IReadOnlyList<AlphabetLetterResponse>>`, sắp theo `DisplayOrder`. Chỉ một query duy nhất — FE lấy cả 29 chữ một lần, chi tiết từng chữ hiển thị client-side.

**Infrastructure:** `AlphabetLetterConfiguration` + seed `HasData` với 29 Guid tĩnh.

**Web.Api:** `GET /alphabet` → 200 danh sách 29 chữ; `RequireAuthorization()` — không có token trả 401.

### 3.4 Dữ liệu seed 29 chữ cái

| # | Chữ | Tên chữ | Cách đọc | Từ ví dụ | Emoji |
|---|-----|---------|----------|----------|-------|
| 1 | A a | a | a | áo | 👕 |
| 2 | Ă ă | á | á | ăn | 🍚 |
| 3 | Â â | ớ | ớ | ấm | 🫖 |
| 4 | B b | bê | bờ | bò | 🐄 |
| 5 | C c | xê | cờ | cá | 🐟 |
| 6 | D d | dê | dờ | dép | 🩴 |
| 7 | Đ đ | đê | đờ | đèn | 💡 |
| 8 | E e | e | e | em bé | 👶 |
| 9 | Ê ê | ê | ê | ếch | 🐸 |
| 10 | G g | giê | gờ | gà | 🐔 |
| 11 | H h | hát | hờ | hoa | 🌸 |
| 12 | I i | i | i | im lặng | 🤫 |
| 13 | K k | ca | cờ | kẹo | 🍬 |
| 14 | L l | e-lờ | lờ | lá | 🍃 |
| 15 | M m | em-mờ | mờ | mèo | 🐱 |
| 16 | N n | en-nờ | nờ | nón | 👒 |
| 17 | O o | o | o | ong | 🐝 |
| 18 | Ô ô | ô | ô | ô tô | 🚗 |
| 19 | Ơ ơ | ơ | ơ | quả mơ | 🍑 |
| 20 | P p | pê | pờ | pin | 🔋 |
| 21 | Q q | quy | quờ | quà | 🎁 |
| 22 | R r | e-rờ | rờ | rùa | 🐢 |
| 23 | S s | ét-sì | sờ | sao | ⭐ |
| 24 | T t | tê | tờ | táo | 🍎 |
| 25 | U u | u | u | uống nước | 🥤 |
| 26 | Ư ư | ư | ư | sư tử | 🦁 |
| 27 | V v | vê | vờ | voi | 🐘 |
| 28 | X x | ích-xì | xờ | xe đạp | 🚲 |
| 29 | Y y | i dài | i | yêu | ❤️ |

Ghi chú: với "ơ" và "ư" không có từ mầm non quen thuộc bắt đầu bằng chữ đó nên dùng từ *chứa* chữ (thông lệ sách mầm non).

### 3.5 Database & Docker

- `dlearning-be/docker-compose.yml`: service `postgres` image `postgres:17-alpine`, port `5432:5432`, `POSTGRES_DB=dlearning`, user/password dev `postgres/postgres`, named volume.
- Connection string trong `appsettings.Development.json` trỏ `localhost:5432/dlearning`.
- Một migration `Initial` (schema + seed alphabet + seed user demo). Ở môi trường Development, Web.Api tự `Migrate()` khi khởi động.
- Integration tests không dùng compose — Testcontainers tự spin Postgres như template.

### 3.6 Tests

- **ArchitectureTests:** giữ nguyên bộ rule của template (đổi namespace).
- **Application.UnitTests:** `RegisterUserCommandHandlerTests` (thành công, email trùng, username trùng), `LoginCommandHandlerTests` (thành công bằng email, thành công bằng username, sai password → InvalidCredentials, user không tồn tại → InvalidCredentials), `GetAlphabetQueryHandlerTests` (trả đúng thứ tự).
- **Api.IntegrationTests:** flow register → login → gọi `GET /alphabet` bằng token nhận 29 chữ; `GET /alphabet` không token → 401; login sai → 401; register email trùng → 409.

## 4. Frontend `dlearning-web`

### 4.1 Nền tảng

Vite + React + TypeScript. Không dùng UI library — CSS thuần (CSS modules) theo design token trích từ `dLearning Login.dc.html`:

- Màu chủ đạo: gradient `#4F46E5 → #6D28D9 → #7C3AED`; nền ngoài `#E9E7E0`; nền form `#FDFDFB`; chữ chính `#1B1B2F`; chữ phụ `#6B6B80`; border input `#E4E2EC`; lỗi `#B91C1C`/nền `#FEF2F2`.
- Font: `Sora` (heading, 700–800) + `Plus Jakarta Sans` (body) từ Google Fonts.
- Bo góc lớn (12–22px), shadow mềm, animation `fadeUp`/`floaty` như design.

### 4.2 Clean architecture FE

```
src/
├── domain/
│   ├── models/          User.ts, AlphabetLetter.ts, AuthSession.ts
│   └── repositories/    AuthRepository.ts, AlphabetRepository.ts   (interface/port)
├── application/
│   └── usecases/        login.ts, register.ts, logout.ts, getAlphabet.ts
├── infrastructure/
│   ├── http/            HttpClient.ts   (fetch wrapper: baseUrl /api, gắn Bearer token, 401 → callback đăng xuất)
│   ├── repositories/    ApiAuthRepository.ts, ApiAlphabetRepository.ts
│   └── storage/         TokenStorage.ts (remember=true → localStorage, false → sessionStorage)
├── presentation/
│   ├── auth/            AuthProvider.tsx (context giữ session), RequireAuth.tsx (guard route)
│   ├── pages/           LoginPage/, RegisterPage/, AlphabetPage/
│   ├── components/      (BrandPane, TextField, PrimaryButton, LetterCard, LetterDetail…)
│   └── router.tsx       react-router: /login, /register, /alphabet (guarded), / → redirect
├── di/container.ts      composition root: new HttpClient → repositories → truyền vào use cases/context
└── main.tsx
```

Quy tắc phụ thuộc: `presentation → application → domain`; `infrastructure → domain`; chỉ `di/` biết cách ráp infrastructure vào application. Domain/application không import React.

### 4.3 Màn hình

1. **LoginPage** — bám sát design desktop + responsive:
   - Trái: brand pane gradient tím — logo dLearning, "Học tập không giới hạn.", 2 thẻ nổi (floaty), số liệu 20.000+ học viên / 850+ khóa học.
   - Phải: form "Đăng nhập" — input identifier (label "Email, số điện thoại hoặc tên đăng nhập"), password có nút show/hide, checkbox "Ghi nhớ đăng nhập", link "Quên mật khẩu?" (chưa hoạt động), nút "Đăng nhập" (loading → "Đang đăng nhập…"), hộp lỗi đỏ khi thất bại, nút Google/Facebook **chỉ là UI**, link "Đăng ký ngay" → `/register`.
2. **RegisterPage** — cùng layout brand pane; form: Tên hiển thị, Email, Username, Mật khẩu; thành công → tự đăng nhập luôn rồi vào `/alphabet`.
3. **AlphabetPage** — màn học cho bé mầm non:
   - Header: chào theo tên (`DisplayName` từ session), nút đăng xuất.
   - Lưới 29 `LetterCard` màu sắc (palette xoay vòng theo `displayOrder`, lấy cảm hứng các gradient cover trong design), chữ hoa to + từ ví dụ + emoji.
   - Bấm thẻ → `LetterDetail` (modal overlay): chữ hoa & thường cỡ lớn, tên chữ, cách đọc, từ ví dụ + emoji, nút đóng, nút chuyển chữ trước/sau.
   - Loading state khi fetch; lỗi mạng hiện thông báo thân thiện + nút thử lại.

### 4.4 Auth flow & error handling

- Login/Register thành công → lưu `{ token, user }` qua `TokenStorage` → `AuthProvider` cập nhật → điều hướng `/alphabet`.
- `RequireAuth` chưa có session → redirect `/login`.
- Mọi response 401 từ API → xóa session, quay về `/login`.
- Lỗi API hiển thị dạng hộp lỗi đỏ theo design; message lấy từ ProblemDetails của BE (fallback thông báo chung tiếng Việt).
- Dev: Vite proxy `/api` → `http://localhost:5113` (port profile `http` của template, bỏ prefix `/api`), không cần CORS.

### 4.5 FE tests

Vitest: unit test cho use cases (`login`, `register`, `getAlphabet`) với repository giả — kiểm tra lưu session, ném lỗi đúng loại; test `TokenStorage` chọn local/sessionStorage theo remember.

## 5. Cách chạy end-to-end

```bash
# 1. Database
cd dlearning-be && docker compose up -d

# 2. Backend (tự migrate + seed khi chạy Development)
dotnet run --project src/Web.Api

# 3. Frontend
cd ../dlearning-web && npm install && npm run dev   # http://localhost:5173
```

**Tiêu chí hoàn thành:**
- Đăng nhập bằng `demo@dlearning.vn` (hoặc username `demo`) / `Demo@123`, hoặc tự đăng ký tài khoản mới.
- Vào màn bảng chữ cái thấy đủ 29 chữ lấy từ API, bấm xem chi tiết từng chữ.
- `dotnet test` xanh cả 3 project (integration cần Docker); `npm test` xanh.
- Gọi `GET /alphabet` không token trả 401.

## 6. Ngoài phạm vi (lần này không làm)

- OAuth Google/Facebook (nút chỉ là UI), quên mật khẩu, refresh token, đăng nhập bằng số điện thoại.
- Dashboard khóa học trong design (stats, courses) — chỉ làm màn bảng chữ cái.
- Audio phát âm, theo dõi tiến độ học của bé (ứng viên cho phase sau).
