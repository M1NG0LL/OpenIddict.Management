# OpenIddict.Management.Storage.EfCore

[![NuGet](https://img.shields.io/nuget/v/Mingoll.OpenIddict.Management.Storage.EfCore.svg?style=flat-square&label=NuGet)](https://www.nuget.org/packages/Mingoll.OpenIddict.Management.Storage.EfCore)
[![Downloads](https://img.shields.io/nuget/dt/Mingoll.OpenIddict.Management.Storage.EfCore.svg?style=flat-square)](https://www.nuget.org/packages/Mingoll.OpenIddict.Management.Storage.EfCore)
[![Target Frameworks](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)

`OpenIddict.Management.Storage.EfCore` (`Mingoll.OpenIddict.Management.Storage.EfCore`) delivers complete Entity Framework Core persistence implementations for the OpenIddict Management ecosystem.

Install this package when using EF Core (SQL Server, PostgreSQL, SQLite, MySQL) as your backing database. It provides out-of-the-box management store implementations (`EfCoreApplicationManagementStore`, `EfCoreTokenStore`, `EfCoreAuthorizationStore`, `EfCoreScopeManagementStore`), enhanced entity types with auditing and metadata properties, and fluent `ModelBuilder` extensions.

---

## Installation

Install via the .NET CLI:

```bash
dotnet add package Mingoll.OpenIddict.Management.Storage.EfCore
```

Or via the Visual Studio Package Manager Console:

```powershell
Install-Package Mingoll.OpenIddict.Management.Storage.EfCore
```

---

## Prerequisites & Dependencies

### Supported Frameworks
- **.NET 8.0** (LTS)
- **.NET 9.0** (STS)
- **.NET 10.0**

### Required Package Dependencies
- [`Mingoll.OpenIddict.Management.Core`](../OpenIddict.Management.Core/README.md) (Abstractions, models, contracts)
- `OpenIddict.EntityFrameworkCore` (>= 6.x / 7.x)
- `Microsoft.EntityFrameworkCore.Relational` (>= 8.x / 9.x)

### Companion Database Providers
You must reference your preferred EF Core database provider in your host application:
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Sqlite`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Pomelo.EntityFrameworkCore.MySql`

---

## Configuration & Setup

### 1. Configure the `DbContext`

Extend `DbContext` and register the enhanced management entities. In `OnModelCreating`, invoke `modelBuilder.UseOpenIddictManagement()`:

```csharp
using Microsoft.EntityFrameworkCore;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Extensions;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ManagementApplication<Guid>> Applications => Set<ManagementApplication<Guid>>();
    public DbSet<ManagementAuthorization<Guid>> Authorizations => Set<ManagementAuthorization<Guid>>();
    public DbSet<ManagementScope<Guid>> Scopes => Set<ManagementScope<Guid>>();
    public DbSet<ManagementToken<Guid>> Tokens => Set<ManagementToken<Guid>>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configures table mappings, indexes, and relationships for OpenIddict Management entities
        modelBuilder.UseOpenIddictManagement();

        // For custom primary key types (e.g., int, string):
        // modelBuilder.UseOpenIddictManagement<string>();
    }
}
```

### 2. Register Services in `Program.cs`

#### Option A: Unified Registration (Core + EF Core Stores) — Recommended

```csharp
using Microsoft.EntityFrameworkCore;
using OpenIddict.Management.Storage.EfCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Registers Core services, EF Core stores, and OpenIddict Core entity mappings in one call
builder.Services.AddOpenIddictManagement<ApplicationDbContext>(options =>
{
    options.RoutePrefix = "/api/management";
    options.RequireHttps = true;
});
```

#### Option B: Fluent Builder Registration

```csharp
using OpenIddict.Management.Extensions;
using OpenIddict.Management.Storage.EfCore.Extensions;

builder.Services.AddOpenIddictManagement()
    .AddEfCoreStores<ApplicationDbContext>();
```

#### Option C: Custom Primary Key Type (`TKey`)

If your database uses `string` or `int` identifiers instead of `Guid`:

```csharp
builder.Services.AddOpenIddictManagement<ApplicationDbContext, string>();
```

#### Option D: Individual Store Override

If you need to customize an individual store while preserving the rest:

```csharp
builder.Services.AddApplicationManagementStore<MyCustomAppStore>();
builder.Services.AddTokenStore<MyCustomTokenStore>();
```

---

## Usage Example

### Injecting and Using Management Services

All contracts from `OpenIddict.Management.Core` are registered in Dependency Injection and backed by EF Core stores:

```csharp
using OpenIddict.Abstractions;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Results;

public class IdentityAdministrationService(
    IApplicationManagementService applicationService,
    IOpenIddictTokenManager tokenManager,
    IScopeManagementService scopeService)
{
    public async Task CreateClientAppAsync(CancellationToken cancellationToken)
    {
        // 1. Create a client application
        var createResult = await applicationService.CreateAsync(new ApplicationCreateDto
        {
            ClientId = "mobile-portal-app",
            DisplayName = "Mobile Portal Application",
            ClientSecret = "SuperSecretKey987654!",
            Environment = ApplicationEnvironment.Production,
            ContactEmail = "security@enterprise.local",
            Organization = "Enterprise Mobile Team",
            Tags = ["Mobile", "iOS", "Android"],
            Permissions = [
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Prefixes.Scope + "api_read"
            ],
            RedirectUris = ["https://mobile.enterprise.local/callback"]
        }, cancellationToken);

        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException($"Creation failed: {createResult.Error.Description}");
        }
    }

    public async Task RevokeUserSessionsAsync(string userId, CancellationToken cancellationToken)
    {
        // 2. Revoke all active tokens for a specific user across all clients
        Result<RevocationResultDto> revocationResult = await tokenManager.RevokeByUserAsync(userId, cancellationToken);
        if (revocationResult.IsSuccess)
        {
            Console.WriteLine($"Revoked {revocationResult.Value.RevokedTokensCount} tokens for user {userId}.");
        }
    }

    public async Task<PagedResult<TokenListDto>> QueryActiveTokensAsync(CancellationToken cancellationToken)
    {
        // 3. Query tokens with typed filters
        var filter = new TokenFilterRequest
        {
            Page = 1,
            PageSize = 25,
            Status = TokenStatus.Valid,
            Type = OpenIddictConstants.TokenTypeHints.AccessToken
        };

        var result = await tokenManager.ListTokensAsync(filter, cancellationToken);
        return result.Value;
    }
}
```

---

## Related Packages

| Package | Purpose |
|---|---|
| [`Mingoll.OpenIddict.Management.Core`](../OpenIddict.Management.Core/README.md) | Shared contracts, DTOs, domain models, login engine, and options. |
| [`Mingoll.OpenIddict.Management.Endpoints`](../OpenIddict.Management.Endpoints/README.md) | Ready-to-map ASP.NET Core Minimal API endpoint groups. |
| [`Mingoll.OpenIddict.Management.Dashboard`](../OpenIddict.Management.Dashboard/README.md) | Razor Class Library admin dashboard UI for visual database administration. |
