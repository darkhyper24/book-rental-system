# Book Rental System — Architecture

A REST API where users browse books and request rentals, admins add books and approve or reject requests, and a single seeded super admin manages admins.

**Stack:** .NET 10 · ASP.NET Core Minimal APIs · Entity Framework Core 10 · PostgreSQL (Npgsql) · ASP.NET Core Identity · JWT Bearer authentication

---

## 1. N-tier layout

The solution (`BookRental.slnx`) has three projects. Each layer may only reference the layer below it.

```
┌──────────────────────────────────────────────┐
│ BookRental.PresentationLayer   (Web API)     │  HTTP: routes, auth, status codes
│   references ↓ BusinessLogicLayer, ↓ DAL     │  (DAL reference only for DI setup and role constants)
├──────────────────────────────────────────────┤
│ BookRental.BusinessLogicLayer  (class lib)   │  Business rules, DTOs, token creation
│   references ↓ DataAccessLayer               │
├──────────────────────────────────────────────┤
│ BookRental.DataAccessLayer     (class lib)   │  Entities, DbContext, repositories, migrations
└──────────────────────────────────────────────┘
                     ↓
                PostgreSQL
```

| Layer | Knows about |
|---|---|
| Presentation | HTTP, routing, path gateways, claims |
| Business Logic | Rules (ISBN unique, request cap, roles on register), DTO ↔ entity mapping |
| Data Access | EF Core, LINQ queries, PostgreSQL specifics |

Entities never leave the server: every response is a DTO.

---

## 2. Folder structure

```
book-rental-system/
├── BookRental.slnx
├── .env                       (git-ignored: secrets)
├── ARCHITECTURE.md
└── src/
    ├── BookRental.PresentationLayer/
    │   ├── Endpoints/
    │   │   ├── AuthEndpoints.cs         /api/auth        (public)
    │   │   ├── BookEndpoints.cs         /api/books       (public)
    │   │   ├── UserEndpoints.cs         /api/user        (gateway: User)
    │   │   ├── AdminEndpoints.cs        /api/admin       (gateway: Admin)
    │   │   └── SuperAdminEndpoints.cs   /api/superadmin  (gateway: SuperAdmin)
    │   ├── Extensions/
    │   │   ├── IdentityExtensions.cs           AddIdentityServices(): Identity options, roles, EF stores
    │   │   ├── JwtAuthenticationExtensions.cs  AddJwtAuthentication(): JwtSettings + JwtBearer validation
    │   │   └── ServiceCollectionExtensions.cs  AddApplicationServices(): services, repositories, validation
    │   ├── Middleware/
    │   │   └── RequireRoleMiddleware.cs role gateway used by the app.Map branches
    │   ├── Properties/launchSettings.json
    │   └── Program.cs                   composition root: DI, auth, pipeline
    │
    ├── BookRental.BusinessLogicLayer/
    │   ├── DTOs/
    │   │   ├── Auth/    RegisterDto, LoginDto, CreateAdminDto, AuthResponseDto
    │   │   ├── Books/   CreateBookDto, BookDto
    │   │   └── Rents/   PendingRentDto, MyRentDto
    │   ├── Services/    AuthService, TokenService, IBookService + BookService, IRentService + RentService
    │   └── Settings/    JwtSettings
    │
    └── BookRental.DataAccessLayer/
        ├── Constants/   RoleNames
        ├── Entities/    ApplicationUser, Book, Rent (+ RentStatus enum)
        ├── Data/
        │   ├── AppDbContext.cs
        │   ├── Configurations/   ApplicationUserConfiguration, BookConfiguration, RentConfiguration
        │   └── Migrations/
        └── Repositories/ IBookRepository + BookRepository, IRentRepository + RentRepository
```

---

## 3. Request and response flow

### 3.1 Request flow (inbound)

Every request passes through the same stages, in this order. Each stage can stop the request early and send a response without reaching the next stage.

| # | Stage | Where | What it does | Can stop with |
|---|---|---|---|---|
| 1 | Routing | Added automatically by `WebApplication` at the start of the pipeline | Matches method + path against the registered endpoints (full paths, e.g. `POST /api/admin/books`) | **404** no endpoint matches (in a protected area the gateway answers **401** / **403** first), **405** wrong method |
| 2 | `UseHttpsRedirection` | `Program.cs` | Redirects HTTP to HTTPS when an HTTPS port is configured (inactive on the `http` launch profile) | 307 redirect |
| 3 | `UseAuthentication` | `Program.cs` | Reads `Authorization: Bearer <jwt>`, validates signature, lifetime, issuer and audience, and builds `HttpContext.User` from the claims. A missing or invalid token leaves an anonymous user (it does not reject here) | — |
| 4 | `app.Map` gateway | `Program.cs` + `Middleware/RequireRoleMiddleware.cs` | Only for `/api/user`, `/api/admin`, `/api/superadmin`: checks `HttpContext.User` against the area's allowed role | **401** not logged in, **403** wrong role |
| 5 | Endpoint execution | `UseEndpoints` (inside the branch) or the end of the main pipeline (public areas) | Runs the matched endpoint | — |
| 6 | Parameter binding | Minimal APIs | Fills the handler's parameters: DTO from the JSON body, `Guid` ids from the route, `ClaimsPrincipal` from `HttpContext.User`, services (`AuthService`, `IBookService`, `IRentService`) from dependency injection | **400** malformed JSON (an id that is not a GUID never gets here: the `:guid` route constraint gives **404**) |
| 7 | DTO validation | `AddValidation()` | Runs the DataAnnotations on the request DTO (`[Required]`, `[EmailAddress]`, `[Range]`, …) | **400** `ValidationProblem` |
| 8 | Handler | `Endpoints/*.cs` | Calls one service method | — |
| 9 | Service | `BusinessLogicLayer/Services` | Applies the business rules, maps DTO → entity | returns errors (see 3.2) |
| 10 | Repository | `DataAccessLayer/Repositories` (Identity's `UserManager` for users) | Runs EF Core queries/commands | — |
| 11 | Database | PostgreSQL | Executes SQL; constraints (unique indexes) are the last line of defense | unique violation → handled by `TryAddRentAsync` for rents; not caught for books (**500**, see section 8) |

### 3.2 Response flow (outbound)

The answer travels back through the same layers in reverse, each layer changing the shape of the data:

| # | Layer | Receives | Hands back |
|---|---|---|---|
| 1 | Database → Repository | Rows | **Entities** (`Book`, `Rent`, `ApplicationUser`), or counts / booleans / rows-changed numbers for checks and atomic updates |
| 2 | Repository → Service | Entities, `BookWithPendingCount`, booleans, `ApproveResult` | **DTOs** for queries (`BookDto`, `PendingRentDto`, `MyRentDto`, `AuthResponseDto`), or a **`List<string>` of errors** for commands (empty list = success). Login returns `AuthResponseDto?` (`null` = invalid credentials) |
| 3 | Service → Handler | DTOs / error list / `null` | An **`IResult`**: `Results.Ok(dto)`, `Results.Created()`, `Results.BadRequest(errors)`, `Results.Unauthorized()` |
| 4 | Handler → Client | `IResult` | ASP.NET serializes the body to **JSON** (property names in camelCase, e.g. `copiesAvailable`) and writes the status code |

Responses that never reach a handler come from earlier stages: **404** / **405** (routing), **401/403** (gateway, with `WWW-Authenticate: Bearer`), **400** `ValidationProblem` (validation).

| Outcome | Status | Body |
|---|---|---|
| Query succeeded | 200 | DTO or DTO array |
| Command succeeded | 201 (create) / 200 (approve, reject) | empty |
| Business rule failed | 400 | `string[]` of messages, e.g. `["A book with this ISBN already exists."]` |
| Request DTO invalid | 400 | `{"title": "...", "errors": {"Field": ["message"]}}` |
| Wrong credentials | 401 | empty |
| No / invalid token | 401 | empty, `WWW-Authenticate: Bearer` header |
| Wrong role | 403 | empty |
| Unknown route or non-GUID id | 404 (in a protected area, only after the gateway lets the caller through) | empty |
| Wrong method | 405 | empty |
| Unhandled exception | 500 | empty (exception details in Development) |

Entities never appear in a response: the service maps them to DTOs, so fields like `PasswordHash` never leave the server. The business layer never builds HTTP results; only the handler decides status codes.

### 3.3 Example: a user requests a rental

```
REQUEST   POST /api/user/books/{bookId}/rent          Authorization: Bearer <jwt>
   │
   ├─ routing             → UserEndpoints.RequestRent
   ├─ UseAuthentication   → HttpContext.User { sub, role = User, ... }
   ├─ /api/user gateway   → role User ✓
   ├─ binding             → bookId from route, ClaimsPrincipal, IRentService from DI (→ RentService)
   │
PresentationLayer   RequestRent: userId = user.FindFirstValue("sub")
BusinessLogicLayer  IRentService.RequestRentAsync(userId, bookId)   (implemented by RentService)
                      book exists? → no duplicate pending? → pending < copies?
DataAccessLayer     IBookRepository.GetBookByIdAsync, IRentRepository.HasPendingRentAsync,
                    CountPendingRentsForBookAsync, TryAddRentAsync
PostgreSQL          INSERT INTO "Rents" (...) VALUES (...)

RESPONSE
PostgreSQL          1 row inserted (or unique violation → TryAddRentAsync returns false)
DataAccessLayer     true / false
BusinessLogicLayer  []  or  ["No copies available to request."] ...
PresentationLayer   [] → Results.Created()        errors → Results.BadRequest(errors)
Client              201 (empty)                   400 ["..."]
```

### 3.4 Example: login

```
REQUEST   POST /api/auth/login   { "email": "...", "password": "..." }
   ├─ routing → AuthEndpoints.Login   (public: no gateway)
   ├─ binding → LoginDto from JSON;  validation → [Required], [EmailAddress]
PresentationLayer   Login → AuthService.LoginUserAsync(dto)
BusinessLogicLayer  UserManager.FindByEmailAsync → CheckPasswordAsync → GetRolesAsync
                    → TokenService.CreateToken(user, roles)

RESPONSE
BusinessLogicLayer  AuthResponseDto { token, expiresAt, name, email, roles }   or  null
PresentationLayer   Results.Ok(dto)                                            or  Results.Unauthorized()
Client              200 {"token":"eyJ...","expiresAt":"...","name":"...","email":"...","roles":["User"]}   or  401
```

---

## 4. Authentication and authorization

### Identity
- `ApplicationUser : IdentityUser<Guid>` adds a display `Name`. `UserName` = email; login is by email.
- Registered with `AddIdentityCore<ApplicationUser>()` (no cookies) + `AddRoles<IdentityRole<Guid>>()` + `AddEntityFrameworkStores<AppDbContext>()`.
- Options: unique email required, password ≥ 8 characters (digit, upper and lower case required; symbol not required). Lockout is not used.
- `AuthService` uses `UserManager` only (no `SignInManager`, which lives in the ASP.NET Core framework the business layer does not reference).

### Roles
Stored in Identity's `AspNetRoles` / `AspNetUserRoles` tables, names in `RoleNames`:

| Role | Gets it by | Can |
|---|---|---|
| `User` | Registering (`/api/auth/register` hard-codes this role) | Request rentals, see own requests |
| `Admin` | Created by the SuperAdmin | Add books, approve/reject requests |
| `SuperAdmin` | Seeded once by migration | Create admins only (no admin powers: cannot add books or approve requests) |

Registration DTOs have no role field, so a client cannot register as an admin (over-posting protection). The seeded SuperAdmin must create an Admin before books can be added.

### JWT
- Created at login by `TokenService` (HS256, key from `.env`).
- Claims: `sub` (user id), `email`, `name` (display name), `jti` (unique token id), one `role` claim per role, `iss`, `aud`, plus `exp`/`iat`/`nbf` added automatically.
- Validation (`Extensions/JwtAuthenticationExtensions.cs`, called from `Program.cs` as `builder.Services.AddJwtAuthentication(builder.Configuration)`): signature, lifetime, issuer (`Jwt:Issuer`) and audience (`Jwt:Audience`); `ClockSkew` 30 s, `MapInboundClaims = false`, `RoleClaimType = "role"`, `NameClaimType = "name"`.
- Roles are copied into the token at login, so a role change takes effect after the user logs in again.

### Path gateways (`app.Map` + `RequireRoleMiddleware`)

Each protected URL area is an `app.Map` branch with its own middleware pipeline. The branch's `RequireRoleMiddleware` runs before any endpoint in that area:

| Path prefix | Allowed roles | Middleware result |
|---|---|---|
| `/api/auth`, `/api/books` | Anyone (no branch) | — |
| `/api/user` | `User` | not logged in → 401 (`ChallengeAsync`), other role → 403 (`ForbidAsync`), else continue |
| `/api/admin` | `Admin` | same |
| `/api/superadmin` | `SuperAdmin` | same |

```csharp
app.Map("/api/admin", branch =>
{
    branch.UseRequireRole(RoleNames.Admin);
    branch.UseRouting();
    branch.UseEndpoints(_ => { });
});
```

Endpoints are registered on `app` with their full paths (`MapGroup("/api/admin")`), so routing matches them before the branch runs, and the branch's `UseEndpoints` executes the matched endpoint. Because templates use full paths, an endpoint can't be reached from the root (`POST /books` → 404) or from another area's prefix. This replaces ASP.NET authorization policies: there is no `AddAuthorization` / `UseAuthorization`.

---

## 5. API endpoints

| Method | Route | Access | Story | Success | Errors |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Anyone | US-0 | 201 | 400 validation / Identity errors |
| POST | `/api/auth/login` | Anyone | US-0 | 200 `AuthResponseDto` | 400 validation, 401 bad credentials |
| POST | `/api/superadmin/admins` | SuperAdmin | US-0 | 201 | 400, 401, 403 |
| GET | `/api/books` | Anyone | US-1, US-2 | 200 `BookDto[]` | — |
| POST | `/api/admin/books` | Admin | US-1 | 201 | 400 (validation, duplicate ISBN), 401, 403, 500 (simultaneous duplicate ISBN, see section 8) |
| POST | `/api/user/books/{bookId}/rent` | User | US-2 | 201 | 400 (not found, duplicate pending, no copies), 401, 403 |
| GET | `/api/user/rents` | User | US-4 | 200 `MyRentDto[]` | 401, 403 |
| GET | `/api/admin/rents/pending` | Admin | US-3 | 200 `PendingRentDto[]` | 401, 403 |
| POST | `/api/admin/rents/{rentId}/approve` | Admin | US-3 | 200 | 400 (not found, already handled, no copies), 401, 403 |
| POST | `/api/admin/rents/{rentId}/reject` | Admin | US-3 | 200 | 400 (not found, already handled), 401, 403 |

Request DTOs are validated automatically with DataAnnotations via `builder.Services.AddValidation()` (returns 400 `ValidationProblem` before the handler runs).

---

## 6. Data model

```
AspNetUsers 1 ──── * Rents * ──── 1 Books
     │
     * AspNetUserRoles * ──── 1 AspNetRoles   (User, Admin, SuperAdmin)
```

**AspNetUsers** (Identity columns + `Name`)

| Column | Type | Notes |
|---|---|---|
| `Id` | uuid | PK |
| `Name` | varchar(50) | Display name (custom) |
| `Email` / `UserName` | varchar(256) | Same value; `NormalizedUserName` has the unique index, `NormalizedEmail` a non-unique one (Identity also checks email uniqueness before saving) |
| `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, … | | Managed by Identity |

**Books**

| Column | Type | Notes |
|---|---|---|
| `Id` | uuid | PK |
| `Title` | varchar(100) | Required |
| `Author` | varchar(50) | Required |
| `Isbn` | varchar(20) | Required, unique |
| `CopiesAvailable` | int | Decreased by 1 on each approval |

**Rents**

| Column | Type | Notes |
|---|---|---|
| `Id` | uuid | PK |
| `BookId` | uuid | FK → `Books` |
| `UserId` | uuid | FK → `AspNetUsers` |
| `RequestDate` | timestamptz | UTC |
| `Status` | int | `RentStatus` enum |

- `RentStatus`: `Pending = 0`, `Approved = 1`, `Rejected = 2` (stored as integer).
- Indexes on `Rents`: `BookId`, `UserId`, and a **filtered unique index** `(UserId, BookId) WHERE "Status" = 0` (one pending request per user per book).
- Foreign keys from `Rents` cascade on delete.
- Entity rules live in `Data/Configurations/*` (Fluent API), applied via `ApplyConfigurationsFromAssembly`.

### Migrations

| Migration | Purpose |
|---|---|
| `first_migration` | Identity tables, `Books`, `Rents` |
| `SeedRolesAndSuperAdmin` | Hand-written: inserts the 3 roles and the SuperAdmin, reading `SUPERADMIN_*` from `.env` at `database update` time and hashing the password with Identity's `PasswordHasher` (no credentials in the repository) |
| `AddUniquePendingRentIndex` | Filtered unique index on pending rents |

---

## 7. Business rules

| Rule | Enforced in |
|---|---|
| Title, author, ISBN, copies required; copies ≥ 1 | `CreateBookDto` annotations |
| ISBN unique | `BookService` check + unique index on `Books.Isbn` |
| One pending request per user per book | `RentService` check + filtered unique index |
| **Request cap (rule B):** a request is refused when `pending requests for the book ≥ CopiesAvailable`. This keeps *pending ≤ copies* unless requests arrive simultaneously (see section 8). A rejection frees a slot. | `RentService.RequestRentAsync` |
| Approving sets `Approved` and decreases copies by 1; blocked if no copies remain | `RentRepository.ApproveRentAsync` |
| Rejecting sets `Rejected`; copies unchanged | `RentRepository.RejectRentAsync` |
| `BookDto.IsAvailable` = a request slot is free (`CopiesAvailable − pending > 0`) — drives the "Request rental" button | `BookService.GetAllBooksAsync` |

---

## 8. Concurrency protection

| Scenario | Protection |
|---|---|
| Same request approved twice, or approved and rejected at the same time | Atomic conditional update: `UPDATE "Rents" SET "Status" = … WHERE "Id" = @id AND "Status" = 0` (`ExecuteUpdateAsync`); 0 rows changed → "already handled" |
| Approving with no copies left | Atomic `UPDATE "Books" SET "CopiesAvailable" = "CopiesAvailable" - 1 WHERE "Id" = @id AND "CopiesAvailable" > 0`; status change and decrement run in one transaction, rolled back if either fails |
| User double-clicks "request" | Filtered unique index; the losing insert is caught (`PostgresErrorCodes.UniqueViolation`) and reported as "already pending" |

### Known gaps

| Scenario | Current behavior |
|---|---|
| Same ISBN added by simultaneous requests | The unique index keeps the data correct, but `AddBookAsync` does not catch the violation, so losing requests can get **500** instead of 400 |
| Different users request the last copy simultaneously | The count and the insert are not atomic, so pending requests can exceed the copies. Approval still cannot take copies below 0; surplus requests cannot be approved |

---

## 9. Configuration and running

Secrets live in a git-ignored `.env` at the repository root, loaded first in `Program.cs` by `DotNetEnv.Env.TraversePath().Load()`. Double underscores map to configuration sections (`Jwt__Key` → `Jwt:Key`).

```
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=book_rental_system;Username=postgres;Password=...
SUPERADMIN_NAME=Super Admin
SUPERADMIN_EMAIL=superadmin@bookrental.com
SUPERADMIN_PASSWORD=...
Jwt__Key=<random, at least 32 bytes>
Jwt__Issuer=BookRentalApi
Jwt__Audience=BookRentalClient
Jwt__ExpiresInMinutes=60
```

```powershell
dotnet ef database update -p src/BookRental.DataAccessLayer -s src/BookRental.PresentationLayer
dotnet run --project src/BookRental.PresentationLayer --launch-profile http
# API at http://localhost:5198
```

All Microsoft packages are pinned to 10.0.x (11.x requires .NET 11).

---

## 10. Design decisions

- Dependency inversion for books and rents: endpoints depend on `IBookService` / `IRentService`, and those services depend on `IBookRepository` / `IRentRepository`. Each interface lives in the same project as its implementation.
- `AuthService` and `TokenService` are concrete classes without interfaces.
- Services and repositories are registered with `AddScoped` in `Extensions/ServiceCollectionExtensions.cs` (`AddApplicationServices()`), as interface → implementation where an interface exists, e.g. `AddScoped<IBookService, BookService>()`.
- `Program.cs` stays short: Identity, JWT and application services are each configured in their own extension method under `Extensions/`.
- Minimal APIs grouped per area instead of controllers; handlers are named static methods in `Endpoints/*`.
- Role checks are path gateways (`app.Map` branches + `RequireRoleMiddleware`) instead of authorization policies.
- Repositories wrap `AppDbContext` for books and rents; Identity's `UserManager` plays that role for users.
- Read queries use `AsNoTracking()`.
