using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Endpoints.Extensions;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Extensions;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenAPI specification generation (.NET native OpenAPI)
builder.Services.AddOpenApi();

// Configure SQLite Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "DataSource=openiddict-api.db";
builder.Services.AddDbContext<SampleApiDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

// Configure OpenIddict Server & Validation
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<SampleApiDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token")
               .SetIntrospectionEndpointUris("/connect/introspect")
               .SetRevocationEndpointUris("/connect/revocation");

        options.AllowClientCredentialsFlow()
               .AllowAuthorizationCodeFlow();

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// Configure OpenIddict Management Suite (automatically registers EF Core stores & default entity types)
builder.Services.AddOpenIddictManagement<SampleApiDbContext>();

var app = builder.Build();

// Ensure database created and seed initial demonstration data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SampleApiDbContext>();
    await context.Database.EnsureCreatedAsync();

    var appService = scope.ServiceProvider.GetRequiredService<IApplicationManagementService>();
    var existingApp = await appService.GetByClientIdAsync("sample-api-client");
    if (!existingApp.IsSuccess)
    {
        await appService.CreateAsync(new ApplicationCreateDto
        {
            ClientId = "sample-api-client",
            ClientSecret = "sample-api-secret-67890",
            DisplayName = "Sample Minimal API Client",
            Environment = ApplicationEnvironment.Development,
            Permissions = [
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                OpenIddictConstants.Permissions.Prefixes.Scope + "api_read"
            ]
        });
    }

    var scopeService = scope.ServiceProvider.GetRequiredService<IScopeManagementService>();
    var existingScope = await scopeService.GetByNameAsync("api_read");
    if (!existingScope.IsSuccess)
    {
        await scopeService.CreateAsync("api_read", "Read Access", "Grants read access", ["api_resource"]);
    }
}

app.UseRouting();

// Map OpenAPI schema, Scalar interactive documentation, and Swagger UI
app.MapOpenApi();
app.MapScalarApiReference();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "OpenIddict Management API v1");
    options.RoutePrefix = "swagger";
});

// Map OpenIddict Management Minimal API Endpoints
app.MapOpenIddictManagementEndpoints(options =>
{
    options.RoutePrefix = "/api/management";
    options.RequireAuthorization = false; // Set to true in production
});

app.MapGet("/", () => Results.Content(@"<!DOCTYPE html>
<html>
<head>
    <title>Sample Minimal API — OpenIddict Management</title>
    <style>
        body { font-family: system-ui, sans-serif; background: #0f172a; color: #f8fafc; padding: 3rem; }
        .container { max-width: 800px; margin: 0 auto; background: #1e293b; padding: 2.5rem; border-radius: 12px; border: 1px solid #334155; }
        h1 { margin-top: 0; }
        ul { line-height: 1.8; }
        a { color: #38bdf8; text-decoration: none; font-weight: 500; }
        a:hover { text-decoration: underline; }
        code { background: #0f172a; padding: 0.2rem 0.5rem; border-radius: 4px; color: #a5b4fc; }
        .badge-links { display: flex; gap: 0.75rem; margin: 1rem 0; flex-wrap: wrap; }
        .badge-link { display: inline-block; background: #2563eb; color: #fff; padding: 0.5rem 1rem; border-radius: 6px; font-weight: 600; text-decoration: none; }
        .badge-link.swagger { background: #059669; }
        .badge-link:hover { opacity: 0.9; text-decoration: none; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>OpenIddict Management Minimal API</h1>
        <p>Explore and test the API using interactive documentation:</p>
        <div class='badge-links'>
            <a href='/scalar/v1' class='badge-link'>Open Scalar API Reference</a>
            <a href='/swagger' class='badge-link swagger'>Open Swagger UI</a>
        </div>
        <p>Available Management REST Endpoints:</p>
        <ul>
            <li><code>GET /api/management/applications</code> — List registered client applications</li>
            <li><code>GET /api/management/scopes</code> — List configured scopes</li>
            <li><code>POST /api/management/revocation/by-user</code> — Revoke all tokens for a user</li>
            <li><code>POST /api/management/revocation/by-client</code> — Revoke all tokens for a client</li>
            <li><code>POST /api/management/revocation/by-session</code> — Revoke session authorizations</li>
        </ul>
    </div>
</body>
</html>", "text/html"));

app.Run();

public class SampleApiDbContext(DbContextOptions<SampleApiDbContext> options) : DbContext(options)
{
    public DbSet<ManagementApplication<Guid>> Applications => Set<ManagementApplication<Guid>>();
    public DbSet<ManagementAuthorization<Guid>> Authorizations => Set<ManagementAuthorization<Guid>>();
    public DbSet<ManagementScope<Guid>> Scopes => Set<ManagementScope<Guid>>();
    public DbSet<ManagementToken<Guid>> Tokens => Set<ManagementToken<Guid>>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.UseOpenIddictManagement();
    }
}
