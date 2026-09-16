# OpenIddict.Management.Endpoints

[![NuGet](https://img.shields.io/nuget/v/Mingoll.OpenIddict.Management.Endpoints.svg?style=flat-square&label=NuGet)](https://www.nuget.org/packages/Mingoll.OpenIddict.Management.Endpoints)
[![Downloads](https://img.shields.io/nuget/dt/Mingoll.OpenIddict.Management.Endpoints.svg?style=flat-square)](https://www.nuget.org/packages/Mingoll.OpenIddict.Management.Endpoints)
[![Target Frameworks](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)

`OpenIddict.Management.Endpoints` (`Mingoll.OpenIddict.Management.Endpoints`) delivers pre-packaged ASP.NET Core Minimal API endpoint route groups for managing OpenIddict client applications, scopes, tokens, authorizations, active user sessions, and background token cleanup jobs.

Install this package when you need a standardized, secured RESTful management API to power a frontend Single Page Application (React, Angular, Vue, Blazor), an external administrative portal, CLI management tools, or automated infrastructure provisioning scripts.

---

## Installation

Install via the .NET CLI:

```bash
dotnet add package Mingoll.OpenIddict.Management.Endpoints
```

Or via the Visual Studio Package Manager Console:

```powershell
Install-Package Mingoll.OpenIddict.Management.Endpoints
```

---

## Prerequisites & Dependencies

### Supported Frameworks
- **.NET 8.0** (LTS)
- **.NET 9.0** (STS)
- **.NET 10.0**

### Required Package Dependencies
- `Microsoft.AspNetCore.App` (Shared Framework)
- [`Mingoll.OpenIddict.Management.Core`](../OpenIddict.Management.Core/README.md) (Abstractions, models, contracts, validation)

### Companion Persistence Packages
This package requires an implementation of the management service contracts registered in your Dependency Injection container. Most applications pair this with [`Mingoll.OpenIddict.Management.Storage.EfCore`](../OpenIddict.Management.Storage.EfCore/README.md).

---

## Configuration & Setup

Map all management endpoint groups using `MapOpenIddictManagementEndpoints()` on `WebApplication` or any `IEndpointRouteBuilder`.

### Service Registration & Endpoint Mapping (`Program.cs`)

```csharp
using Microsoft.EntityFrameworkCore;
using OpenIddict.Management.Endpoints;
using OpenIddict.Management.Endpoints.Extensions;
using OpenIddict.Management.Storage.EfCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Authentication & Authorization policies
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options => { /* Configure token validation */ });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IdentityAdminPolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Administrator");
    });
});

// 2. Register storage and Core services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddOpenIddictManagement<ApplicationDbContext>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// 3. Map all OpenIddict Management Minimal API endpoints
app.MapOpenIddictManagementEndpoints(options =>
{
    options.RoutePrefix = "/api/management";
    options.AuthorizationPolicy = "IdentityAdminPolicy";
    options.RequireAuthorization = true;
    options.Tags = ["OpenIddict Management API"];
});

app.Run();
```

### Configuration via `appsettings.json`

Settings can be stored in configuration files and bound to `ManagementEndpointOptions`:

```json
{
  "ManagementEndpoints": {
    "RoutePrefix": "/api/management",
    "AuthorizationPolicy": "IdentityAdminPolicy",
    "RequireAuthorization": true,
    "Tags": ["OpenIddict Management API"]
  }
}
```

Bind in `Program.cs`:

```csharp
builder.Services.Configure<ManagementEndpointOptions>(
    builder.Configuration.GetSection("ManagementEndpoints"));

// Uses bound options automatically when configure delegate is omitted or null
app.MapOpenIddictManagementEndpoints();
```

---

## Mapped Endpoint Catalog

When mapped, the package exposes the following REST route groups under the configured `RoutePrefix`:

### Applications (`/applications`)
| Method | Route | Description |
|---|---|---|
| `GET` | `/api/management/applications` | Paginated search of applications (supports `search`, `statusFilter`, `environmentFilter`, `tags`). |
| `GET` | `/api/management/applications/{id}` | Get application by ID. |
| `POST` | `/api/management/applications` | Create a new client application with validation. |
| `PUT` | `/api/management/applications/{id}` | Update an existing application's settings and permissions. |
| `DELETE` | `/api/management/applications/{id}` | Delete an application. |
| `POST` | `/api/management/applications/{id}/rotate-secret` | Rotate client credentials. |
| `PATCH` | `/api/management/applications/bulk/status` | Bulk update status (active, suspended, revoked). |

### Scopes (`/scopes`)
| Method | Route | Description |
|---|---|---|
| `GET` | `/api/management/scopes` | Paginated list of registered OAuth/OIDC scopes. |
| `GET` | `/api/management/scopes/{id}` | Get scope details. |
| `POST` | `/api/management/scopes` | Register a new scope with associated resources. |
| `PUT` | `/api/management/scopes/{id}` | Update scope metadata. |
| `DELETE` | `/api/management/scopes/{id}` | Delete a scope. |

### Tokens & Analytics (`/tokens`)
| Method | Route | Description |
|---|---|---|
| `GET` | `/api/management/tokens` | Paginated token query (filter by `status`, `type`, `clientId`, `subject`). |
| `GET` | `/api/management/tokens/counts` | Summary counts of valid, expired, and revoked tokens. |
| `GET` | `/api/management/tokens/counts-by-application` | Token distribution grouped by client application. |
| `GET` | `/api/management/tokens/timeline` | Daily issuance activity over a given date range. |
| `POST` | `/api/management/tokens/extend` | Extend expiration date for specified tokens. |

### Revocation & Sessions (`/revocation`, `/sessions`)
| Method | Route | Description |
|---|---|---|
| `POST` | `/api/management/revocation/by-user` | Revoke all tokens for a specific user ID. |
| `POST` | `/api/management/revocation/by-client` | Revoke all tokens issued to a client ID. |
| `POST` | `/api/management/revocation/by-session` | Revoke authorizations matching a session ID. |
| `POST` | `/api/management/revocation/bulk` | Revoke a list of explicit token IDs. |
| `GET` | `/api/management/sessions` | List active sessions. |
| `DELETE` | `/api/management/sessions/{sessionId}` | Terminate session and revoke related tokens. |

### Overview & Token Cleanup (`/overview`, `/cleanup`)
| Method | Route | Description |
|---|---|---|
| `GET` | `/api/management/overview` | High-level metrics (total apps, active tokens, scopes, health). |
| `GET` | `/api/management/cleanup/status` | Current status and metrics of the background cleanup worker. |
| `POST` | `/api/management/cleanup/run` | Manually trigger a cleanup cycle immediately. |
| `PUT` | `/api/management/cleanup/settings` | Update cleanup job frequency and batch size at runtime. |

---

## Usage Example

### Creating a Client Application via HTTP

```http
POST /api/management/applications HTTP/1.1
Host: localhost:5001
Authorization: Bearer <ADMIN_JWT_TOKEN>
Content-Type: application/json

{
  "clientId": "portal-spa",
  "displayName": "Enterprise Customer Portal",
  "clientType": "public",
  "environment": "Production",
  "redirectUris": [
    "https://portal.enterprise.com/callback"
  ],
  "postLogoutRedirectUris": [
    "https://portal.enterprise.com"
  ],
  "permissions": [
    "ept:token",
    "ept:authorization",
    "gt:authorization_code",
    "gt:refresh_token",
    "rst:code",
    "scp:openid",
    "scp:profile",
    "scp:email"
  ]
}
```

### Revoking All Tokens for a Compromised User

```http
POST /api/management/revocation/by-user HTTP/1.1
Host: localhost:5001
Authorization: Bearer <ADMIN_JWT_TOKEN>
Content-Type: application/json

{
  "userId": "user-84839"
}
```

Response:

```json
{
  "revokedTokensCount": 4,
  "revokedAuthorizationsCount": 2,
  "message": "Successfully revoked all tokens and authorizations for user 'user-84839'."
}
```

---

## Related Packages

| Package | Purpose |
|---|---|
| [`Mingoll.OpenIddict.Management.Core`](../OpenIddict.Management.Core/README.md) | Shared contracts, DTOs, domain models, and validation filters. |
| [`Mingoll.OpenIddict.Management.Storage.EfCore`](../OpenIddict.Management.Storage.EfCore/README.md) | Entity Framework Core persistence implementations backing the API. |
| [`Mingoll.OpenIddict.Management.Dashboard`](../OpenIddict.Management.Dashboard/README.md) | Embedded Razor Class Library admin dashboard UI consuming these services. |
