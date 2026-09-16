# OpenIddict.Management.Dashboard

[![NuGet](https://img.shields.io/nuget/v/Mingoll.OpenIddict.Management.Dashboard.svg?style=flat-square&label=NuGet)](https://www.nuget.org/packages/Mingoll.OpenIddict.Management.Dashboard)
[![Downloads](https://img.shields.io/nuget/dt/Mingoll.OpenIddict.Management.Dashboard.svg?style=flat-square)](https://www.nuget.org/packages/Mingoll.OpenIddict.Management.Dashboard)
[![Target Frameworks](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)

`OpenIddict.Management.Dashboard` (`Mingoll.OpenIddict.Management.Dashboard`) is a turnkey Razor Class Library (RCL) that embeds a full-featured, responsive administrative UI directly into your ASP.NET Core OpenIddict host.

Install this package when you need a ready-to-use graphical interface to visually manage OAuth2/OIDC client applications, inspect tokens, revoke user sessions, configure scopes, and monitor identity server metrics without needing to design, build, or host a separate frontend admin application.

---

## Installation

Install via the .NET CLI:

```bash
dotnet add package Mingoll.OpenIddict.Management.Dashboard
```

Or via the Visual Studio Package Manager Console:

```powershell
Install-Package Mingoll.OpenIddict.Management.Dashboard
```

---

## Prerequisites & Dependencies

### Supported Frameworks
- **.NET 8.0** (LTS)
- **.NET 9.0** (STS)
- **.NET 10.0**

### Required Package Dependencies
- `Microsoft.AspNetCore.App` (Shared Framework / Razor SDK)
- [`Mingoll.OpenIddict.Management.Core`](../OpenIddict.Management.Core/README.md) (Abstractions, models, enums)
- [`Mingoll.OpenIddict.Management.Endpoints`](../OpenIddict.Management.Endpoints/README.md) (Minimal API management endpoints)

### Companion Persistence Packages
The dashboard interacts with management services registered in DI. Ensure a storage provider such as [`Mingoll.OpenIddict.Management.Storage.EfCore`](../OpenIddict.Management.Storage.EfCore/README.md) is registered.

---

## Configuration & Setup

Register the dashboard services using the fluent `.AddDashboard()` method on `OpenIddictManagementBuilder`, and map its routes in the HTTP pipeline using `app.MapOpenIddictManagementDashboard()`.

### Service Registration & Route Mapping (`Program.cs`)

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Management.Dashboard.Extensions;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Storage.EfCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Host Authentication & Admin Policy
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IdentityAdminPolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Administrator");
    });
});

// 2. Configure Database & Management Suite with Dashboard
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddOpenIddictManagement<ApplicationDbContext>()
    .AddDashboard(options =>
    {
        options.PathPrefix = "/admin/identity";
        options.DashboardTitle = "Enterprise Identity Admin";
        options.ExitUrl = "/";
        options.ExitButtonText = "Back to App";
        options.RequireAuthorization = true;
        options.AuthorizationPolicy = "IdentityAdminPolicy";
        options.EnabledFeatures = DashboardFeature.All;
        options.AvailableTags = ["Internal", "Partner", "Production", "Mobile"];
    });

var app = builder.Build();

// 3. Pipeline configuration
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 4. Mount the Dashboard (registers static web assets, middleware, and Razor Pages)
app.MapOpenIddictManagementDashboard();

app.Run();
```

### Configuration via `appsettings.json`

You can define dashboard options in `appsettings.json` and bind them:

```json
{
  "Dashboard": {
    "PathPrefix": "/admin/identity",
    "DashboardTitle": "Enterprise Identity Admin",
    "ExitUrl": "/",
    "ExitButtonText": "Back to App",
    "RequireAuthorization": true,
    "AuthorizationPolicy": "IdentityAdminPolicy",
    "AvailableTags": [
      "Internal",
      "Partner",
      "Production",
      "Mobile"
    ]
  }
}
```

Bind in `Program.cs`:

```csharp
builder.Services.Configure<DashboardOptions>(
    builder.Configuration.GetSection("Dashboard"));
```

---

## Dashboard Configuration Options

The `DashboardOptions` class supports the following settings:

| Property | Type | Default | Description |
|---|---|---|---|
| `PathPrefix` | `string` | `"/management"` | Path prefix where the dashboard UI and static assets are mounted. |
| `DashboardTitle` | `string` | `"OpenIddict Management"` | Brand title displayed in the dashboard navigation header. |
| `ExitUrl` | `string?` | `"/"` | Target URL when the user clicks the exit button. Null/empty hides the button. |
| `ExitButtonText` | `string` | `"Exit Dashboard"` | Display text for the exit link button. |
| `RequireAuthorization` | `bool` | `true` | When true, enforces authorization checks before loading dashboard pages. |
| `AuthorizationPolicy` | `string?` | `null` | Optional ASP.NET Core authorization policy name required to access the UI. |
| `EnabledFeatures` | `DashboardFeature` | `DashboardFeature.All` | Flags enum toggling which dashboard sections and modules are visible. |
| `AvailableTags` | `List<string>` | `[]` | Predefined application tag options displayed when creating or editing apps. |

### Feature Flag Controls (`DashboardFeature`)

The `DashboardFeature` enum allows enabling or disabling specific dashboard modules:

```csharp
// Example: Enable only Application and Scope management, hiding Token Inspector
options.EnabledFeatures = DashboardFeature.ApplicationManagement | DashboardFeature.ScopeManager;
```

Available flags:
- `DashboardFeature.ApplicationManagement` — Client application list, registration, credential rotation, and permission editor.
- `DashboardFeature.TokenInspector` — Live token browser, search, details viewer, and expiration extender.
- `DashboardFeature.SessionManager` — Active session monitoring and revocation by user or session ID.
- `DashboardFeature.ScopeManager` — Scope registry, descriptions, and resource mappings.
- `DashboardFeature.AuditTrail` — High-level metric counters and historical issuance timelines.
- `DashboardFeature.All` — Enables all features (default).

---

## Usage Example

### Navigating the Admin Interface

Once mounted, navigating to `https://localhost:5001/admin/identity` provides:

1. **Dashboard Home (`/admin/identity`)**: Real-time system overview cards (total applications, active access/refresh tokens, registered scopes), token issuance timeline charts, and cleanup worker status.
2. **Applications (`/admin/identity/applications`)**: Search and filter client apps by environment, status, or tag. Create new public/confidential apps with granular OpenIddict permissions, redirect URIs, and secret generation.
3. **Tokens (`/admin/identity/tokens`)**: Inspect active, expired, and revoked tokens. View token details, extend valid expiration dates, or revoke individual tokens.
4. **Scopes (`/admin/identity/scopes`)**: Configure OAuth scopes, assign descriptions, and associate target API resource servers.
5. **Settings (`/admin/identity/settings`)**: Inspect background cleanup worker telemetry, adjust batch pruning parameters, or trigger an immediate cleanup run.

---

## Related Packages

| Package | Purpose |
|---|---|
| [`Mingoll.OpenIddict.Management.Core`](../OpenIddict.Management.Core/README.md) | Underlying domain abstractions, models, enums, and options. |
| [`Mingoll.OpenIddict.Management.Storage.EfCore`](../OpenIddict.Management.Storage.EfCore/README.md) | Entity Framework Core persistence layer powering dashboard data. |
| [`Mingoll.OpenIddict.Management.Endpoints`](../OpenIddict.Management.Endpoints/README.md) | Minimal API REST endpoints utilized for management operations. |
