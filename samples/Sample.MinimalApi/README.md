# Sample.MinimalApi — OpenIddict Management Minimal API Sample

This sample demonstrates exposing RESTful management endpoints using **OpenIddict.Management.Endpoints** in an ASP.NET Core Minimal API application:
- **SQLite** database persistence using Entity Framework Core.
- **OpenIddict Minimal API Endpoints** mounted at `/api/management`.
- Seeded OpenID Connect client application and scopes.
- OpenIddict Server supporting Client Credentials and Authorization Code flows.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Quickstart

1. Navigate to the project folder:
   ```bash
   cd samples/Sample.MinimalApi
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. Open your browser or API client to:
   - Info page: `https://localhost:7002/`
   - **Interactive Scalar API Reference**: `https://localhost:7002/scalar/v1`
   - OpenAPI document: `https://localhost:7002/openapi/v1.json`
   - List applications: `GET https://localhost:7002/api/management/applications`
   - List scopes: `GET https://localhost:7002/api/management/scopes`
