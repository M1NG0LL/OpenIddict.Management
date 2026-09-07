# Database & EF Core Reference

Complete reference for OpenIddict.Management EF Core entities, configurations, and store implementations.

---

## Extended Entities

The library extends standard OpenIddict EF Core entities with management-specific columns. All entities are generic over `TKey` (primary key type) and have a non-generic variant defaulting to `Guid`.

### `ManagementApplication<TKey>`

**Base class:** `OpenIddictEntityFrameworkCoreApplication<TKey, ManagementAuthorization<TKey>, ManagementToken<TKey>>`
**Table:** `OpenIddictApplications`

| Column | Type | Constraints | Description |
|---|---|---|---|
| *(inherited)* | | | All standard OpenIddict application columns (`Id`, `ClientId`, `DisplayName`, `ClientSecret`, `Permissions`, `RedirectUris`, `PostLogoutRedirectUris`, `Requirements`, `Properties`, `Type`, etc.) |
| `Status` | `ApplicationStatus` enum | Indexed | Operational status: `Active` (0), `Disabled` (1), `Deleted` (2) |
| `Environment` | `ApplicationEnvironment` enum | Indexed | Deployment environment: `Development` (0), `Staging` (1), `Production` (2) |
| `AllowedRolesJson` | `string?` | | JSON-serialized list of role names. Access via `GetAllowedRoles()` / `SetAllowedRoles()` |
| `Description` | `string?` | MaxLength 500 | Free-text description |
| `LogoUri` | `string?` | MaxLength 2000 | Logo URL |
| `OwnerUserId` | `string?` | MaxLength 450, Indexed | User who owns this application |
| `CreatedAt` | `DateTimeOffset` | | UTC creation timestamp |
| `LastModifiedAt` | `DateTimeOffset?` | | UTC last modification timestamp |
| `ExtraData` | `string?` | | Arbitrary JSON metadata |
| `Tags` | `List<string>` | EF Core PrimitiveCollection | Arbitrary tags for filtering |

**Helper methods:**
- `bool HasTag(string tag)` — case-insensitive tag lookup
- `List<string> GetAllowedRoles()` — deserializes `AllowedRolesJson`
- `void SetAllowedRoles(IEnumerable<string>? roles)` — serializes roles to `AllowedRolesJson`

### `ManagementAuthorization<TKey>`

**Base class:** `OpenIddictEntityFrameworkCoreAuthorization<TKey, ManagementApplication<TKey>, ManagementToken<TKey>>`

| Column | Type | Description |
|---|---|---|
| *(inherited)* | | All standard OpenIddict authorization columns |
| `CreatedAt` | `DateTimeOffset` | UTC creation timestamp |
| `LastModifiedAt` | `DateTimeOffset?` | UTC last modification timestamp |

### `ManagementScope<TKey>`

**Base class:** `OpenIddictEntityFrameworkCoreScope<TKey>`

| Column | Type | Description |
|---|---|---|
| *(inherited)* | | All standard OpenIddict scope columns |
| `CreatedAt` | `DateTimeOffset` | UTC creation timestamp |
| `LastModifiedAt` | `DateTimeOffset?` | UTC last modification timestamp |

### `ManagementToken<TKey>`

**Base class:** `OpenIddictEntityFrameworkCoreToken<TKey, ManagementApplication<TKey>, ManagementAuthorization<TKey>>`

| Column | Type | Description |
|---|---|---|
| *(inherited)* | | All standard OpenIddict token columns |
| `RevokedAt` | `DateTimeOffset?` | UTC timestamp when the token was revoked |

---

## Entity Configurations

### `ManagementApplicationConfiguration<TKey>`

Applied via `IEntityTypeConfiguration<ManagementApplication<TKey>>`:

```csharp
builder.ToTable("OpenIddictApplications");
builder.Property(a => a.Description).HasMaxLength(500);
builder.Property(a => a.LogoUri).HasMaxLength(2000);
builder.Property(a => a.OwnerUserId).HasMaxLength(450);
builder.PrimitiveCollection(a => a.Tags);
builder.HasIndex(a => a.Status);
builder.HasIndex(a => a.Environment);
builder.HasIndex(a => a.OwnerUserId);
```

### `ManagementAuthorizationConfiguration<TKey>`

Configures `ManagementAuthorization<TKey>` entity mapping.

### `ManagementScopeConfiguration<TKey>`

Configures `ManagementScope<TKey>` entity mapping.

### `ManagementTokenConfiguration<TKey>`

Configures `ManagementToken<TKey>` entity mapping.

---

## ModelBuilder Extensions

### `UseOpenIddictManagement()`

Call in your `DbContext.OnModelCreating` to register all management entities and configurations:

```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);

    builder.UseOpenIddictManagement();       // Guid keys (default)
    // or
    builder.UseOpenIddictManagement<int>();  // Custom key type
}
```

This internally calls:
1. `builder.UseOpenIddict<ManagementApplication<TKey>, ManagementAuthorization<TKey>, ManagementScope<TKey>, ManagementToken<TKey>, TKey>()`
2. Applies `ManagementApplicationConfiguration<TKey>`
3. Applies `ManagementAuthorizationConfiguration<TKey>`
4. Applies `ManagementScopeConfiguration<TKey>`
5. Applies `ManagementTokenConfiguration<TKey>`

---

## Store Implementations

The `Storage.EfCore` package provides three EF Core store implementations:

| Interface | Implementation | Description |
|---|---|---|
| `IApplicationManagementService` | `EfCoreApplicationManagementStore<TContext, TKey>` | Full application CRUD with filtering, pagination, status management |
| `IOpenIddictRevocationManager` | `EfCoreRevocationStore<TContext, TKey>` | Token/authorization revocation, listing, pruning, counts |
| `IScopeManagementService` | `EfCoreScopeManagementStore<TContext, TKey>` | Scope CRUD with pagination and search |

---

## Store Registration

### Default registration (with DbContext)

```csharp
// Registers all three stores with Guid keys
builder.Services.AddOpenIddictManagementStores<AppDbContext>();

// With custom key type
builder.Services.AddOpenIddictManagementStores<AppDbContext, int>();
```

### Via builder chaining

```csharp
builder.Services
    .AddOpenIddictManagement()
    .AddEfCoreStores<AppDbContext>();          // Guid keys

builder.Services
    .AddOpenIddictManagement()
    .AddEfCoreStores<AppDbContext, int>();     // Custom key type
```

### Combined (Core + Stores in one call)

```csharp
builder.Services.AddOpenIddictManagement<AppDbContext>();         // Guid keys
builder.Services.AddOpenIddictManagement<AppDbContext, int>();    // Custom key type
```

### Custom store implementations

Replace individual stores:

```csharp
builder.Services.AddApplicationManagementStore<MyCustomAppStore>();
builder.Services.AddRevocationStore<MyCustomRevocationStore>();
builder.Services.AddScopeManagementStore<MyCustomScopeStore>();
```

Replace all three at once:

```csharp
builder.Services.AddOpenIddictManagementStores<MyAppStore, MyRevStore, MyScopeStore>();
```

---

## Migrations

```bash
dotnet ef migrations add InitOpenIddictManagement \
    --context YourDbContext \
    --output-dir Data/Migrations

dotnet ef database update
```

> **Note:** There is no auto-migration feature. Migrations must be generated and applied manually.

---

## Domain Model Mappers

The `Mappers` directory contains mapping logic between EF Core entities and domain models:

- `ApplicationMapper` — maps between `ManagementApplication<TKey>` entity and `ManagedApplication` / `ApplicationListDto` domain models
- `ScopeMapper` — maps between `ManagementScope<TKey>` entity and `ManagedScope` domain model
