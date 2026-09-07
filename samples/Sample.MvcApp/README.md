# Sample.MvcApp — OpenIddict Management MVC Sample

This sample demonstrates integrating the **OpenIddict.Management** suite within an ASP.NET Core MVC application with:
- **SQL Server** database persistence using Entity Framework Core.
- **Embedded Admin Dashboard** mounted at `/admin/identity`.
- Seeded OpenID Connect client application and scopes.
- OpenIddict Server supporting Authorization Code, Client Credentials, and Refresh Token flows.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Microsoft SQL Server or SQL Server LocalDB (`(localdb)\mssqllocaldb`)

## Quickstart

1. Navigate to the project folder:
   ```bash
   cd samples/Sample.MvcApp
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. Open your browser to:
   - Homepage: `https://localhost:7001/` (or port specified in launch output)
   - Admin Dashboard: `https://localhost:7001/admin/identity`
