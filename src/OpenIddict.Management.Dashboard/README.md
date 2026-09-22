# OpenIddict.Management.Dashboard

[![NuGet](https://img.shields.io/nuget/v/Mingoll.OpenIddict.Management.Dashboard.svg?style=flat-square&label=NuGet)](https://www.nuget.org/packages/Mingoll.OpenIddict.Management.Dashboard)
[![Downloads](https://img.shields.io/nuget/dt/Mingoll.OpenIddict.Management.Dashboard.svg?style=flat-square)](https://www.nuget.org/packages/Mingoll.OpenIddict.Management.Dashboard)
[![Target Frameworks](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)

`OpenIddict.Management.Dashboard` (`Mingoll.OpenIddict.Management.Dashboard`) is a turnkey Razor Class Library (RCL) that embeds a full-featured, responsive administrative UI directly into your ASP.NET Core OpenIddict host.

Install this package when you need a ready-to-use graphical interface to visually manage OAuth2/OIDC client applications, inspect tokens, revoke user sessions, configure scopes, manage background token pruning, audit administrative events, and perform full configuration backup and restoration without designing or hosting a separate frontend application.

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

builder.Services.AddOpenIddictManagement<ApplicationDbContext>(options =>
{
    options.EnableAuditLogging = true;
})
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
// Example: Enable only Application, Scope, and Settings management
options.EnabledFeatures = DashboardFeature.ApplicationManagement | DashboardFeature.ScopeManager;
```

Available flags:
- `DashboardFeature.ApplicationManagement` — Client application list, registration, credential rotation, bulk actions, and permission editor.
- `DashboardFeature.TokenInspector` — Live token browser, search, details viewer, and expiration extender.
- `DashboardFeature.SessionManager` — Active session monitoring and revocation by user, client, or session ID.
- `DashboardFeature.ScopeManager` — Scope registry, descriptions, resource mappings, and bulk deletion.
- `DashboardFeature.AuditTrail` — Security and administrative audit trail with filtering and event inspection.
- `DashboardFeature.All` — Enables all features (default).

---

## Admin Interface Modules

Navigating to your configured `PathPrefix` (e.g., `https://localhost:5001/admin/identity`) provides access to:

### 1. Overview (`/`)
- Real-time metric cards: total applications, active access/refresh tokens, registered scopes, and active authorizations.
- Daily token issuance timeline chart.
- Quick health status and background cleanup worker indicators.

### 2. Applications (`/applications`)
- Paginated table of registered OpenIddict client applications with filtering by environment (`Development`, `Staging`, `Production`), status (`Active`, `Suspended`, `Revoked`), and custom tags.
- Client creation wizard with comprehensive OpenIddict permissions catalog and redirect URI validation.
- Client secret generation and rotation with instant reveal.
- **Bulk Actions**: Batch activate, suspend, or delete multiple applications at once.

### 3. Scopes (`/scopes`)
- Scope registry displaying standard and custom OAuth2/OIDC scopes.
- Detail and edit forms for descriptions and associated target API resource servers.
- **Bulk Actions**: Batch deletion of scopes.

### 4. Tokens & Sessions (`/tokens`, `/sessions`)
- Live token browser with status indicators (`Valid`, `Expired`, `Revoked`).
- Extend token expiration dates or revoke individual tokens.
- Revocation actions targeting all tokens for a user, client, or authorization session.

### 5. Audit Trail (`/audit`)
- Real-time audit log of administrative and security events (application created/modified, secret rotated, bulk operations executed, tokens pruned).
- Filter by category, action type, actor, and status.

### 6. Settings (`/settings`)
- **Configuration Export & Import**:
  - **Export Package**: One-click download of the complete system backup as a structured JSON file (`ManagementExportPackage` with dynamic `VersionPrefix` derived from the assembly metadata, e.g. `1.2.0`). Contains applications, scopes, OpenIddict Server options, and Management settings.
  - **Import Package**: Upload backup `.json` files or paste raw JSON directly.
  - **Granular Restoration**: Selectively toggle whether to overwrite existing items, import applications, import scopes, and import runtime OpenIddict Server and Management options.
- **Background Token Cleanup**:
  - Live worker telemetry (last run time, status, last pruned count, total pruned tokens).
  - Runtime adjustments of cleanup interval schedule and batch pruning size.
  - Trigger immediate on-demand cleanup execution.
- **Audit Logging Configuration**:
  - View operational status of audit trail logging.

---

## Related Packages

| Package | Purpose |
|---|---|
| [`Mingoll.OpenIddict.Management.Core`](../OpenIddict.Management.Core/README.md) | Underlying domain abstractions, models, enums, and options. |
| [`Mingoll.OpenIddict.Management.Storage.EfCore`](../OpenIddict.Management.Storage.EfCore/README.md) | Entity Framework Core persistence layer powering dashboard data. |
| [`Mingoll.OpenIddict.Management.Endpoints`](../OpenIddict.Management.Endpoints/README.md) | Minimal API REST endpoints utilized for management operations. |
