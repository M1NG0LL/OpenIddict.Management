# Configuration Options Reference

Complete reference for all configuration options in OpenIddict.Management.

---

## Overview

OpenIddict.Management provides configuration classes across its layers:

| Options Class | Layer | Configuration Method | Default Scope |
|---|---|---|---|
| [`OpenIddictManagementOptions`](#openiddictmanagementoptions) | Core | `services.AddOpenIddictManagement(options => ...)` | Global / Core behaviors |
| [`ManagementEndpointOptions`](#managementendpointoptions) | Endpoints | `app.MapOpenIddictManagementEndpoints(options => ...)` | REST Minimal API routing & auth |
| [`DashboardOptions`](#dashboardoptions) | Dashboard | `services.AddDashboard(options => ...)` | Razor RCL Admin Dashboard UI & auth |

---

## `OpenIddictManagementOptions`

**Namespace:** `OpenIddict.Management.Options`  
**Package:** `OpenIddict.Management.Core`

Configured when registering OpenIddict.Management services:

```csharp
builder.Services.AddOpenIddictManagement(options =>
{
    options.RoutePrefix = "/api/management";
    options.RequireHttps = true;
    options.DefaultPageSize = 25;
    options.MaxPageSize = 100;
});
```

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `RoutePrefix` | `string` | `"/api/management"` | HTTP API route prefix. |
| `RequireHttps` | `bool` | `true` | Indicates whether endpoints require HTTPS transport security. |
| `DefaultPageSize` | `int` | `20` | Default page size applied to paginated service and store requests when not specified. |
| `MaxPageSize` | `int` | `100` | Maximum page size permitted. Requests exceeding this value are capped at this threshold. |

---

## `ManagementEndpointOptions`

**Namespace:** `OpenIddict.Management.Endpoints`  
**Package:** `OpenIddict.Management.Endpoints`

Configured when mapping the HTTP Minimal API endpoints:

```csharp
app.MapOpenIddictManagementEndpoints(options =>
{
    options.RoutePrefix = "/api/management";
    options.RequireAuthorization = true;
    options.AuthorizationPolicy = "RequireAdminRole";
    options.Tags = ["OpenIddict Management", "Admin"];
});
```

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `RoutePrefix` | `string` | `"/api/management"` | Route prefix for all management endpoint route groups. |
| `AuthorizationPolicy` | `string?` | `null` | ASP.NET Core named authorization policy required to access the endpoints. If `null`, default authentication applies when `RequireAuthorization` is `true`. |
| `RequireAuthorization` | `bool` | `true` | Whether endpoints require authenticated access. When set to `false`, anonymous access is permitted (unless an `AuthorizationPolicy` is set). |
| `Tags` | `string[]` | `["OpenIddict Management"]` | OpenAPI tags applied to all generated endpoint definitions for Swagger / OpenAPI documentation. |

---

## `DashboardOptions`

**Namespace:** `OpenIddict.Management.Dashboard`  
**Package:** `OpenIddict.Management.Dashboard`

Configured when registering the Razor Class Library admin dashboard:

```csharp
builder.Services.AddDashboard(options =>
{
    options.PathPrefix = "/management";
    options.DashboardTitle = "Identity Administration";
    options.ExitUrl = "/";
    options.ExitButtonText = "Exit Dashboard";
    options.RequireAuthorization = true;
    options.AuthorizationPolicy = "IdentityAdminPolicy";
    options.EnabledFeatures = DashboardFeature.ApplicationManagement | DashboardFeature.TokenInspector;
    options.AvailableTags = ["Internal", "Partner", "Public", "Mobile"];
});
```

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `PathPrefix` | `string` | `"/management"` | Path prefix where the dashboard middleware intercepts requests and Razor pages are routed. |
| `DashboardTitle` | `string` | `"OpenIddict Management"` | Title displayed in the dashboard header, navigation bar, and browser tab. |
| `ExitUrl` | `string?` | `"/"` | URL to navigate to when the exit button is clicked. If set to `null` or empty, the exit button is hidden from the dashboard UI. |
| `ExitButtonText` | `string` | `"Exit Dashboard"` | Text label rendered on the exit navigation button. |
| `AuthorizationPolicy` | `string?` | `null` | ASP.NET Core named authorization policy evaluated by `DashboardMiddleware`. If specified, users failing the policy receive HTTP 403 Forbidden. |
| `RequireAuthorization` | `bool` | `true` | Whether authentication is required to access the dashboard. If `true` and no named policy is configured, unauthenticated users receive HTTP 401 Unauthorized. |
| `EnabledFeatures` | `DashboardFeature` | `DashboardFeature.All` | Bitwise flags determining which dashboard tabs and feature modules are active in the UI. |
| `AvailableTags` | `List<string>` | `[]` | Predefined application tag suggestions presented as selectable badges in the application create and edit forms. |

---

## `DashboardFeature`

**Namespace:** `OpenIddict.Management.Enums`  
**Package:** `OpenIddict.Management.Core`

A bitwise `[Flags]` enum used with `DashboardOptions.EnabledFeatures` to enable or disable specific sections of the dashboard UI:

| Flag | Value | Description |
|---|---|---|
| `None` | `0` | Disables all feature modules. |
| `ApplicationManagement` | `1` (`1 << 0`) | Enables application CRUD, listing, search, and configuration views. |
| `TokenInspector` | `2` (`1 << 1`) | Enables token inspection, filtering, and revocation views. |
| `SessionManager` | `4` (`1 << 2`) | Enables user session and authorization tracking and bulk revocation. |
| `ScopeManager` | `8` (`1 << 3`) | Enables scope management views. |
| `AuditTrail` | `16` (`1 << 4`) | Enables audit log viewer views. |
| `All` | `31` | Enables all feature modules (composite of all flags above). |

### Example: Selective Feature Activation

To expose only application and scope management while hiding token and session inspectors:

```csharp
options.EnabledFeatures = DashboardFeature.ApplicationManagement 
                        | DashboardFeature.ScopeManager;
```
