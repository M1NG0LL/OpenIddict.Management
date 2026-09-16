# OpenIddict.Management.Core

[![NuGet](https://img.shields.io/nuget/v/Mingoll.OpenIddict.Management.Core.svg?style=flat-square&label=NuGet)](https://www.nuget.org/packages/Mingoll.OpenIddict.Management.Core)
[![Downloads](https://img.shields.io/nuget/dt/Mingoll.OpenIddict.Management.Core.svg?style=flat-square)](https://www.nuget.org/packages/Mingoll.OpenIddict.Management.Core)
[![Target Frameworks](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)

`OpenIddict.Management.Core` (`Mingoll.OpenIddict.Management.Core`) defines the foundational domain abstractions, service contracts, login and token issuance orchestration pipelines, unified result models, and background token cleanup scheduling for the OpenIddict Management suite.

Install this package when building headless identity services, crafting custom storage adapters (e.g., Dapper, MongoDB, Cosmos DB), or implementing application-specific user authentication pipelines without coupling your domain to ASP.NET Core UI or Entity Framework Core.

---

## Installation

Install via the .NET CLI:

```bash
dotnet add package Mingoll.OpenIddict.Management.Core
```

Or via the Visual Studio Package Manager Console:

```powershell
Install-Package Mingoll.OpenIddict.Management.Core
```

---

## Prerequisites & Dependencies

### Supported Frameworks
- **.NET 8.0** (LTS)
- **.NET 9.0** (STS)
- **.NET 10.0**

### Required Package Dependencies
- `OpenIddict.Abstractions` (>= 6.x / 7.x)
- `OpenIddict.Server` (>= 6.x / 7.x)
- `Microsoft.Extensions.Options`
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.Hosting.Abstractions`

### Companion Packages
- For relational database persistence: install [`Mingoll.OpenIddict.Management.Storage.EfCore`](../OpenIddict.Management.Storage.EfCore/README.md).
- For pre-built Minimal API endpoints: install [`Mingoll.OpenIddict.Management.Endpoints`](../OpenIddict.Management.Endpoints/README.md).
- For an embedded admin UI: install [`Mingoll.OpenIddict.Management.Dashboard`](../OpenIddict.Management.Dashboard/README.md).

---

## Configuration & Setup

Register core services using the `AddOpenIddictManagement()` extension method on `IServiceCollection`. This registers the core login engine (`IOpenIddictLoginEngine`), token principal service (`IOpenIddictTokenService`), background cleanup worker, and configures the fluent `OpenIddictManagementBuilder`.

### Service Registration (`Program.cs`)

```csharp
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Extensions;
using OpenIddict.Management.Models;
using OpenIddict.Management.Options;

var builder = WebApplication.CreateBuilder(args);

// Register Core Management services with options
builder.Services.AddOpenIddictManagement(options =>
{
    options.RoutePrefix = "/api/management";
    options.RequireHttps = true;
})
.AddAuthenticationProvider<AppUserAuthenticationProvider>()
.AddTokenCleanup(options =>
{
    options.IsEnabled = true;
    options.Interval = TimeSpan.FromHours(12);
    options.BatchSize = 250;
    options.IncludeRevoked = true;
});
```

### Configuration via `appsettings.json`

You can bind settings from your application configuration files:

```json
{
  "OpenIddictManagement": {
    "RoutePrefix": "/api/management",
    "RequireHttps": true
  },
  "TokenCleanup": {
    "IsEnabled": true,
    "BatchSize": 250,
    "Interval": "12:00:00",
    "IncludeRevoked": true
  }
}
```

Bind the configuration sections directly in `Program.cs`:

```csharp
builder.Services.Configure<OpenIddictManagementOptions>(
    builder.Configuration.GetSection("OpenIddictManagement"));

builder.Services.Configure<TokenCleanupOptions>(
    builder.Configuration.GetSection("TokenCleanup"));

builder.Services.AddOpenIddictManagement()
    .AddAuthenticationProvider<AppUserAuthenticationProvider>();
```

---

## Usage Example

### 1. Implement `IUserAuthenticationProvider`

Your host application provides authentication logic by implementing `IUserAuthenticationProvider`:

```csharp
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Models;

public sealed class AppUserAuthenticationProvider : IUserAuthenticationProvider
{
    public async Task<LoginResult> AuthenticateAsync(
        LoginContext context,
        CancellationToken cancellationToken = default)
    {
        // Replace with your real user store / ASP.NET Core Identity validation
        if (context.Username == "admin" && context.Password == "P@ssw0rd123!")
        {
            return LoginResult.Success(
                userId: "user-1001",
                username: "admin",
                roles: ["Administrator", "Manager"],
                scopes: ["openid", "profile", "email", "api_access"],
                email: "admin@enterprise.local",
                destinationMode: ClaimDestinationMode.AccessTokenAndIdentityToken
            );
        }

        return LoginResult.Failed("invalid_grant", "The username or password is incorrect.");
    }
}
```

### 2. Coordinate Login & Issue Tokens with `IOpenIddictLoginEngine` and `IOpenIddictTokenService`

In your OpenIddict authorization or token endpoint:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Models;

app.MapPost("/connect/custom-login", async (
    [FromBody] LoginContext request,
    IOpenIddictLoginEngine loginEngine,
    IOpenIddictTokenService tokenService,
    CancellationToken cancellationToken) =>
{
    // Orchestrate credential verification
    var result = await loginEngine.AuthenticateAsync(request, cancellationToken);
    if (!result.Succeeded)
    {
        return Results.BadRequest(new { error = result.ErrorHeader, description = result.ErrorMessage });
    }

    // Construct OpenIddict-compliant ClaimsPrincipal with appropriate claim destinations
    ClaimsPrincipal principal = await tokenService.CreatePrincipalAsync(result, cancellationToken);

    // Return SignIn result for OpenIddict Server handler
    return Results.SignIn(principal, properties: null, OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});
```

### 3. Programmatically Trigger Token Cleanup

```csharp
app.MapPost("/api/cleanup/trigger", async (ITokenCleanupJobManager cleanupManager) =>
{
    var cleanupResult = await cleanupManager.RunCleanupNowAsync();
    return cleanupResult.IsSuccess
        ? Results.Ok(new { PrunedTokens = cleanupResult.Value })
        : Results.BadRequest(new { Error = cleanupResult.Error.Description });
});
```

---

## Related Packages

| Package | Purpose |
|---|---|
| [`Mingoll.OpenIddict.Management.Storage.EfCore`](../OpenIddict.Management.Storage.EfCore/README.md) | Entity Framework Core persistence layer, store implementations, and database entities. |
| [`Mingoll.OpenIddict.Management.Endpoints`](../OpenIddict.Management.Endpoints/README.md) | Pre-configured Minimal API route handlers for application, scope, and token operations. |
| [`Mingoll.OpenIddict.Management.Dashboard`](../OpenIddict.Management.Dashboard/README.md) | Embedded Razor Class Library admin UI for visual identity administration. |
