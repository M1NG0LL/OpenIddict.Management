# OpenIddict.Management

> **Identity management made simple.**
> A high-level, developer-friendly abstraction layer over [OpenIddict](https://github.com/openiddict/openiddict-core) that eliminates boilerplate while preserving full underlying capabilities—similar to how [Hangfire](https://www.hangfire.io/) simplifies background job orchestration.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## Table of Contents

- [Why OpenIddict.Management?](#why-openiddictmanagement)
- [Packages](#packages)
- [Installation](#installation)
- [Service Registration](#service-registration)
- [Configuration Options](#configuration-options)
- [Login Pipeline](#login-pipeline)
- [Token Principal Builder](#token-principal-builder)
- [REST API Endpoints](#rest-api-endpoints)
- [Admin Dashboard](#admin-dashboard)
- [EF Core / Database Setup](#ef-core--database-setup)
- [Result Pattern & Error Handling](#result-pattern--error-handling)
- [Pagination](#pagination)
- [What the Library Handles vs What You Must Handle](#what-the-library-handles-vs-what-you-must-handle)
- [Samples](#samples)
- [Target Framework](#target-framework)
- [Contributing](#contributing)
- [License](#license)

---

## Why OpenIddict.Management?

OpenIddict is powerful but verbose. This library wraps the ceremony into clean APIs:

| Pain Point | OpenIddict (raw) | OpenIddict.Management |
|---|---|---|
| Application CRUD | Manual `IOpenIddictApplicationManager` calls | `IApplicationManagementService` with typed DTOs |
| Token / session revocation | Multi-step token lookup + deletion | `IOpenIddictRevocationManager.RevokeByUserAsync(userId)` |
| Token principal construction | Manual `ClaimsIdentity` + destination wiring | `IOpenIddictTokenService.CreatePrincipal(loginResult)` |
| Admin UI | Build from scratch | Embedded Razor Class Library dashboard |
| Scope CRUD | Manual `IOpenIddictScopeManager` calls | `IScopeManagementService` with pagination & search |

---

## Packages

| Package | Description | Dependencies |
|---|---|---|
| `OpenIddict.Management.Core` | Contracts, models, DTOs, login engine, token service, `Result<T>` pattern, options, builder, validation | `OpenIddict.Abstractions`, `OpenIddict.Server` |
| `OpenIddict.Management.Storage.EfCore` | EF Core entities, store implementations, `ModelBuilder` extensions, entity configurations | Core, `OpenIddict.EntityFrameworkCore` |
| `OpenIddict.Management.Endpoints` | Minimal API endpoint groups with validation filters & exception mapping | Core |
| `OpenIddict.Management.Dashboard` | Razor Class Library admin UI (pages, layout, middleware, static assets) | Core, Endpoints |

---

## Installation

```bash
# Core only (if building custom storage)
dotnet add package OpenIddict.Management.Core

# Core + EF Core storage (most common)
dotnet add package OpenIddict.Management.Storage.EfCore

# REST API endpoints
dotnet add package OpenIddict.Management.Endpoints

# Admin dashboard UI
dotnet add package OpenIddict.Management.Dashboard
```

---

## Service Registration

The library provides multiple registration methods depending on your needs.

### Option A: Combined registration (Core + EF Core stores) — Recommended

```csharp
builder.Services
    .AddOpenIddictManagement<AppDbContext>()         // Core + EF Core stores (Guid keys)
    .AddAuthenticationProvider<MyAuthProvider>();     // Register your login provider

// With a custom key type:
builder.Services
    .AddOpenIddictManagement<AppDbContext, int>();    // Core + EF Core stores (int keys)
```

### Option B: Separate registration

```csharp
builder.Services.AddOpenIddictManagement(options =>
{
    options.RoutePrefix = "/api/management";
    options.DefaultPageSize = 25;
});
builder.Services.AddOpenIddictManagementStores<AppDbContext>();
```

### Option C: Fluent builder chaining

```csharp
builder.Services
    .AddOpenIddictManagement()
    .AddEfCoreStores<AppDbContext>()
    .AddAuthenticationProvider<MyAuthProvider>()
    .AddTokenService<MyCustomTokenService>()
    .AddLoginEngine<MyCustomLoginEngine>();
```

### What gets registered

`AddOpenIddictManagement()` registers:
- `IOpenIddictLoginEngine` / `IOpenIddictLoginEngine<LoginContext>` → `OpenIddictLoginEngine`
- `IOpenIddictTokenService` → `OpenIddictTokenService`
- `TimeProvider.System`
- `OpenIddictManagementOptions` via `IOptions<T>`

`AddOpenIddictManagementStores<TContext>()` additionally registers:
- `IApplicationManagementService` → `EfCoreApplicationManagementStore`
- `IOpenIddictRevocationManager` → `EfCoreRevocationStore`
- `IScopeManagementService` → `EfCoreScopeManagementStore`
- OpenIddict Core with EF Core, using the extended management entities

---

## Configuration Options

OpenIddict.Management provides configurable options across its layers:

- **`OpenIddictManagementOptions`** — Core routing defaults, HTTPS enforcement, and pagination limits (`RoutePrefix`, `RequireHttps`, `DefaultPageSize`, `MaxPageSize`). Configured via `AddOpenIddictManagement(options => ...)`.
- **`ManagementEndpointOptions`** — HTTP Minimal API endpoint route prefix, authorization policy, and OpenAPI tags (`RoutePrefix`, `AuthorizationPolicy`, `RequireAuthorization`, `Tags`). Configured via `MapOpenIddictManagementEndpoints(options => ...)`.
- **`DashboardOptions`** — UI path prefix, title, exit URL, authorization policy, feature flags (`DashboardFeature`), and selectable tags. Configured via `AddDashboard(options => ...)`.

> **📖 Full configuration reference:** See [OPTIONS.md](OPTIONS.md) for complete options properties, default values, and usage examples.

---

## Login Pipeline

The login system uses a two-layer architecture: **your app provides authentication logic**, the library orchestrates it.

### `IUserAuthenticationProvider` (you implement this)

Implement `IUserAuthenticationProvider` (uses built-in `LoginContext`) or `IUserAuthenticationProvider<TContext>` for a custom login model. Register via `builder.AddAuthenticationProvider<T>()`.

`LoginContext` provides: `Username`, `Password`, `RememberMe`, `TwoFactorCode`, `TwoFactorRecoveryCode`, `ClientApplicationId`, `IPAddress`, `UserAgent`.

### `LoginResult` (return from your provider)

Factory methods: `LoginResult.Success(...)`, `LoginResult.InvalidCredentials(...)`, `LoginResult.LockedOut(...)`, `LoginResult.TwoFactorRequired(...)`, `LoginResult.Failed(...)`.

Carries token metadata: `UserId`, `Username`, `Email`, `Roles`, `Scopes`, `Resources`, `Claims`, `ExtraData`, `CustomProperties`, `DestinationMode`.

### `IOpenIddictLoginEngine` (orchestration layer)

The default `OpenIddictLoginEngine` validates `LoginContext` fields, then delegates to your `IUserAuthenticationProvider`. Replace entirely via `builder.AddLoginEngine<TEngine>()`.

### Custom login context

Use your own model instead of `LoginContext` by implementing `IUserAuthenticationProvider<TCustomModel>` and registering via `builder.AddAuthenticationProvider<TProvider, TCustomModel>()`.

### PKCE validation

When you register an `IUserAuthenticationProvider`, the library adds a startup validator ensuring Authorization Code Flow is enabled, PKCE is required, and an authorization endpoint URI is configured.

---

## Token Principal Builder

`IOpenIddictTokenService` builds OpenIddict-compliant `ClaimsPrincipal` instances from `TokenCreationParameters` or `LoginResult`.

The default `OpenIddictTokenService`:
1. Creates a `ClaimsIdentity` with `OpenIddict.Server.AspNetCore` as authentication scheme
2. Adds `sub`, `name`, `email`, `role` claims with proper destinations
3. Processes custom `Claims` and `ExtraData` (supports dictionaries, POCOs, primitives)
4. Sets scopes and resources on the identity
5. Routes claims via `ClaimDestinationMode`: `AccessToken`, `IdentityToken`, or `AccessTokenAndIdentityToken`

---

## REST API Endpoints

Map endpoints with:

```csharp
app.MapOpenIddictManagementEndpoints(options =>
{
    options.RoutePrefix = "/api/management";
    options.AuthorizationPolicy = "AdminPolicy";
});
```

### Application Endpoints (`/api/management/applications`)

| Method | Route | Name | Description |
|---|---|---|---|
| `GET` | `/` | `ListApplications` | Paginated list with filters: `status`, `environment`, `tags`, `search`, `sortBy`, `sortDescending` |
| `GET` | `/{id}` | `GetApplicationById` | Get by ID → 200 or 404 |
| `POST` | `/` | `CreateApplication` | Create → 201 |
| `PUT` | `/{id}` | `UpdateApplication` | Update → 200 or 404 |
| `DELETE` | `/{id}?hard=false` | `DeleteApplication` | Soft/hard delete → 204 or 404 |
| `PATCH` | `/{id}/status?status=Active` | `UpdateApplicationStatus` | Update status → 204 or 404 |
| `POST` | `/{id}/secret` | `UpdateClientSecret` | Update client secret |

### Scope Endpoints (`/api/management/scopes`)

| Method | Route | Name | Description |
|---|---|---|---|
| `GET` | `/` | `ListScopes` | Paginated list |
| `GET` | `/{id}` | `GetScopeById` | Get by ID |
| `GET` | `/by-name/{name}` | `GetScopeByName` | Get by unique name |
| `POST` | `/` | `CreateScope` | Create → 201 |
| `PUT` | `/{id}` | `UpdateScope` | Update |
| `DELETE` | `/{id}` | `DeleteScope` | Delete → 204 |

### Token Endpoints (`/api/management/tokens`)

| Method | Route | Name | Description |
|---|---|---|---|
| `DELETE` | `/{id}` | `RevokeTokenById` | Revoke single token by ID |

### Revocation Endpoints (`/api/management/revocation`)

| Method | Route | Name | Description |
|---|---|---|---|
| `POST` | `/by-user` | `RevokeByUser` | Body: `{ "userId": "..." }` |
| `POST` | `/by-client` | `RevokeByClient` | Body: `{ "clientId": "..." }` |
| `POST` | `/by-session` | `RevokeBySession` | Body: `{ "userId": "...", "authorizationId": "..." }` |
| `POST` | `/prune` | `PruneExpiredTokens` | Remove expired/revoked tokens from storage |

### Endpoint Filters

All endpoints include:
- **`ValidationEndpointFilter`** — validates required fields, string lengths, URI formats, whitespace checks
- **`ExceptionMappingEndpointFilter`** — maps domain exceptions to HTTP status codes (404, 409, 400)
- **`ResultExtensions`** — `Result<T>.ToHttpResult()` maps `EntityNotFound` → 404, `DuplicateEntity` → 409, `ValidationFailed` → 400

---

## Admin Dashboard

The dashboard is a **Razor Class Library** with embedded pages, layout, CSS, and JavaScript.

### Registration

```csharp
builder.Services
    .AddOpenIddictManagement<AppDbContext>()
    .AddDashboard(options =>
    {
        options.PathPrefix = "/admin/identity";
        options.DashboardTitle = "My Identity Server";
        options.AuthorizationPolicy = "AdminPolicy";
        options.EnabledFeatures = DashboardFeature.All;
        options.AvailableTags = ["internal", "external", "partner"];
    });

app.MapOpenIddictManagementDashboard();
```

### Dashboard Pages

| Page | URL | Description |
|---|---|---|
| **Overview** | `/{prefix}` | Metric cards: active apps, total apps, scopes, tokens, revoked tokens, active authorizations |
| **Applications** | `/{prefix}/Applications` | Paginated list with status/environment badges, search, filters |
| **Application Details** | `/{prefix}/Applications/Details/{id}` | Read-only detail view |
| **Application Create** | `/{prefix}/Applications/Create` | Create form with permission catalog |
| **Application Edit** | `/{prefix}/Applications/Edit/{id}` | Edit form |
| **Scopes** | `/{prefix}/Scopes` | Paginated scope list |
| **Scope Edit** | `/{prefix}/Scopes/Edit/{id}` | Edit scope |
| **Token Inspector** | `/{prefix}/Tokens` | Token listing with filters, revocation, bulk actions |

### Dashboard Middleware

`DashboardMiddleware` intercepts requests under the `PathPrefix` and enforces authorization:
- Named `AuthorizationPolicy` → evaluates the policy (403 on failure)
- No policy → requires `IsAuthenticated` (401 on failure)

### `OpenIddictPermissionCatalog`

Static catalog of standard OpenIddict permissions for UI selection, categorized into: Endpoints, Grant Types, Response Types, Standard Scopes.

---

## EF Core / Database Setup

The library extends standard OpenIddict entities with management columns (`Status`, `Environment`, `AllowedRolesJson`, `Description`, `Tags`, `CreatedAt`, `RevokedAt`, etc.) and provides EF Core store implementations.

```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);
    builder.UseOpenIddictManagement();       // Guid keys
    // or
    builder.UseOpenIddictManagement<int>();  // Custom key type
}
```

```bash
dotnet ef migrations add InitOpenIddictManagement --context YourDbContext
dotnet ef database update
```

> **📖 Full database reference:** See [DATABASE.md](DATABASE.md) for complete entity schemas, column types, indexes, configurations, store implementations, and custom store registration.

---

## Result Pattern & Error Handling

All service methods return `Result` or `Result<T>` instead of throwing exceptions.

```csharp
var result = await appService.GetByIdAsync("123");
if (result.IsSuccess)
    var app = result.Value;
else
    var error = result.Error;  // ManagementError { Header, Description, Details? }
```

`ManagementError` factories: `EntityNotFound(...)`, `DuplicateEntity(...)`, `ValidationFailed(...)`, `Custom(...)`.

Exception hierarchy: `ManagementException` → `EntityNotFoundException`, `DuplicateEntityException`, `ValidationException`, `RevocationException`.

---

## Pagination

`PagedRequest` — 1-indexed `PageIndex` (default 1), `PageSize` (default 20, max 100), `Search`, `SortBy`, `SortDescending`. Strongly-typed sort via `.WithSort(ApplicationSortField.DisplayName)`.

`PagedResult<T>` — `Items`, `PageIndex`, `PageSize`, `TotalCount`, `TotalPages`, `HasPreviousPage`, `HasNextPage`.

---

## What the Library Handles vs What You Must Handle

### The library handles

- ✅ Application CRUD (create, read, update, soft/hard delete, status management)
- ✅ Scope CRUD (create, read, update, delete)
- ✅ Token listing, filtering, inspection, revocation, pruning
- ✅ `ClaimsPrincipal` construction with proper OpenIddict claim destinations
- ✅ Login orchestration via `IOpenIddictLoginEngine`
- ✅ PKCE validation at startup
- ✅ EF Core entity configuration, model building, and store implementations
- ✅ REST API endpoints with validation and exception mapping
- ✅ Admin dashboard UI with overview metrics
- ✅ Dashboard authorization middleware
- ✅ Pagination, sorting, and filtering

### You must handle

- ❌ **User authentication logic** — implement `IUserAuthenticationProvider`
- ❌ **OpenIddict Server/Validation configuration** — flows, signing keys, endpoint URIs
- ❌ **ASP.NET Core Identity** — user creation, password hashing, 2FA
- ❌ **Authorization policies** — define the admin policy
- ❌ **DbContext creation** — with `builder.UseOpenIddictManagement()`
- ❌ **EF Core migrations** — generate and apply manually
- ❌ **Database provider** — PostgreSQL, SQL Server, SQLite, etc.
- ❌ **OpenIddict authorization/token endpoints** — `/connect/authorize`, `/connect/token`
- ❌ **Consent pages** — custom consent UI if needed

---

## Samples

Working sample projects are available in the [samples/](samples/) directory:

| Sample | Description |
|---|---|
| [Sample.MinimalApi](samples/Sample.MinimalApi/) | Minimal API setup with SQLite, management endpoints, and dashboard |
| [Sample.MvcApp](samples/Sample.MvcApp/) | MVC application with full OpenIddict server + management integration |

---

## Target Framework

- **.NET 10.0**
- **C# latest** — primary constructors, collection expressions
- **Entity Framework Core 10.x**
- **OpenIddict 5.8.0**

---

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) before submitting a pull request.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'feat: add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
