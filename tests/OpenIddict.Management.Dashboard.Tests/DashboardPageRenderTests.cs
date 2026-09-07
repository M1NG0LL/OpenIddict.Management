using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Management.Dashboard.Extensions;
using OpenIddict.Management.Extensions;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Extensions;
using Xunit;

namespace OpenIddict.Management.Dashboard.Tests;

public class DashboardPageRenderTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public DashboardPageRenderTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Theory]
    [InlineData("/management")]
    [InlineData("/management/Applications")]
    [InlineData("/management/Applications/Create")]
    [InlineData("/management/Tokens")]
    [InlineData("/management/Scopes")]
    [InlineData("/management/Scopes/Edit")]
    public async Task DashboardPages_RenderSuccessfully(string path)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddDbContext<DashboardTestDbContext>(opts => opts.UseSqlite(_connection));
        builder.Services.AddOpenIddictManagementStores<DashboardTestDbContext>();
        builder.Services.AddOpenIddictManagement()
            .AddDashboard(opts => opts.RequireAuthorization = false);

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DashboardTestDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        app.UseRouting();
        app.MapOpenIddictManagementDashboard(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();

        await app.StopAsync();
    }

    [Fact]
    public async Task ApplicationsPage_WithSeededApplications_RendersPaginationSuccessfully()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddDbContext<DashboardTestDbContext>(opts => opts.UseSqlite(_connection));
        builder.Services.AddOpenIddictManagementStores<DashboardTestDbContext>();
        builder.Services.AddOpenIddictManagement()
            .AddDashboard(opts => opts.RequireAuthorization = false);

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DashboardTestDbContext>();
            await db.Database.EnsureCreatedAsync();

            db.Applications.Add(new ManagementApplication
            {
                Id = Guid.NewGuid(),
                ClientId = "test-client-id",
                DisplayName = "Test Client",
                CreatedAt = DateTimeOffset.UtcNow,
                Permissions = "[]"
            });
            await db.SaveChangesAsync();
        }

        app.UseRouting();
        app.MapOpenIddictManagementDashboard(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/management/Applications");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("test-client-id");
        content.Should().Contain("Showing page");
        content.Should().Contain("pagination-container");

        await app.StopAsync();
    }

    [Fact]
    public async Task OverviewPage_QuickActionLinks_ContainConfiguredPrefix()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddDbContext<DashboardTestDbContext>(opts => opts.UseSqlite(_connection));
        builder.Services.AddOpenIddictManagementStores<DashboardTestDbContext>();
        builder.Services.AddOpenIddictManagement()
            .AddDashboard(opts =>
            {
                opts.PathPrefix = "/admin/identity";
                opts.RequireAuthorization = false;
            });

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DashboardTestDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        app.UseRouting();
        app.MapOpenIddictManagementDashboard(opts =>
        {
            opts.PathPrefix = "/admin/identity";
            opts.RequireAuthorization = false;
        });

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/admin/identity");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("href=\"/admin/identity/Applications/Create\"");
        content.Should().Contain("href=\"/admin/identity/Scopes\"");
        content.Should().Contain("href=\"/admin/identity/Tokens\"");
        content.Should().NotContain("href=\"/admin/identity/Sessions\"");

        await app.StopAsync();
    }

    [Fact]
    public async Task ApplicationsPage_RendersBulkManagementAndDeactivateButton()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddDbContext<DashboardTestDbContext>(opts => opts.UseSqlite(_connection));
        builder.Services.AddOpenIddictManagementStores<DashboardTestDbContext>();
        builder.Services.AddOpenIddictManagement()
            .AddDashboard(opts => opts.RequireAuthorization = false);

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DashboardTestDbContext>();
            await db.Database.EnsureCreatedAsync();

            db.Applications.Add(new ManagementApplication
            {
                Id = Guid.NewGuid(),
                ClientId = "active-test-client",
                DisplayName = "Active Client",
                CreatedAt = DateTimeOffset.UtcNow,
                Permissions = "[]"
            });
            await db.SaveChangesAsync();
        }

        app.UseRouting();
        app.MapOpenIddictManagementDashboard(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/management/Applications");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Bulk Status Management");
        content.Should().Contain("Deactivate");

        await app.StopAsync();
    }

    [Fact]
    public async Task CreateApplicationPage_RendersPermissionCheckboxesAndPresets()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddDbContext<DashboardTestDbContext>(opts => opts.UseSqlite(_connection));
        builder.Services.AddOpenIddictManagementStores<DashboardTestDbContext>();
        builder.Services.AddOpenIddictManagement()
            .AddDashboard(opts => opts.RequireAuthorization = false);

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DashboardTestDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        app.UseRouting();
        app.MapOpenIddictManagementDashboard(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/management/Applications/Create");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Choose Permissions");
        content.Should().Contain("name=\"SelectedPermissions\"");
        content.Should().Contain("applyPermissionPreset");

        await app.StopAsync();
    }

    [Fact]
    public async Task TokensPage_RendersTokenTableAndFilterControls()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddDbContext<DashboardTestDbContext>(opts => opts.UseSqlite(_connection));
        builder.Services.AddOpenIddictManagementStores<DashboardTestDbContext>();
        builder.Services.AddOpenIddictManagement()
            .AddDashboard(opts => opts.RequireAuthorization = false);

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DashboardTestDbContext>();
            await db.Database.EnsureCreatedAsync();

            var clientApp = new ManagementApplication
            {
                Id = Guid.NewGuid(),
                ClientId = "test-token-client",
                DisplayName = "Test Token Client"
            };
            db.Applications.Add(clientApp);

            db.Tokens.Add(new ManagementToken
            {
                Id = Guid.NewGuid(),
                Subject = "user-test-456",
                Application = clientApp,
                Status = "valid",
                Type = "access_token",
                CreatedAt = DateTimeOffset.UtcNow,
                Payload = "ey-sample-serialized-data-12345"
            });
            await db.SaveChangesAsync();
        }

        app.UseRouting();
        app.MapOpenIddictManagementDashboard(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/management/Tokens");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Token Inspector & Revocation");
        content.Should().Contain("Revoke Filtered Tokens");
        content.Should().Contain("All Tokens");
        content.Should().Contain("Valid Tokens");
        content.Should().Contain("Expired Tokens");
        content.Should().Contain("Revoked Tokens");
        content.Should().NotContain("Revoke by ID");
        content.Should().Contain("Serialized Data");
        content.Should().Contain("ey-sample-serialized-data-12345");
        content.Should().Contain("user-test-456");
        content.Should().Contain("test-token-client");

        await app.StopAsync();
    }

    [Fact]
    public async Task CreateApplicationPage_RendersAvailableTagsAndCustomScopes()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddDbContext<DashboardTestDbContext>(opts => opts.UseSqlite(_connection));
        builder.Services.AddOpenIddictManagementStores<DashboardTestDbContext>();
        builder.Services.AddOpenIddictManagement()
            .AddDashboard(opts =>
            {
                opts.RequireAuthorization = false;
                opts.AvailableTags = ["Mobile", "Internal", "Partner"];
            });

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DashboardTestDbContext>();
            await db.Database.EnsureCreatedAsync();

            db.Scopes.Add(new ManagementScope
            {
                Id = Guid.NewGuid(),
                Name = "orders_api",
                DisplayName = "Orders Service API",
                Description = "Access to orders operations"
            });
            await db.SaveChangesAsync();
        }

        app.UseRouting();
        app.MapOpenIddictManagementDashboard(opts =>
        {
            opts.RequireAuthorization = false;
            opts.AvailableTags = ["Mobile", "Internal", "Partner"];
        });

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/management/Applications/Create");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        // Predefined tags
        content.Should().Contain("Application Tags");
        content.Should().Contain("Mobile");
        content.Should().Contain("Internal");
        content.Should().Contain("Partner");
        content.Should().Contain("name=\"SelectedTags\"");

        // Custom scopes
        content.Should().Contain("Custom Scopes");
        content.Should().Contain("Orders Service API");
        content.Should().Contain("scp:orders_api");

        await app.StopAsync();
    }

    [Fact]
    public async Task ApplicationDetails_RendersTagsSuccessfully()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddDbContext<DashboardTestDbContext>(opts => opts.UseSqlite(_connection));
        builder.Services.AddOpenIddictManagementStores<DashboardTestDbContext>();
        builder.Services.AddOpenIddictManagement()
            .AddDashboard(opts => opts.RequireAuthorization = false);

        var app = builder.Build();

        var appId = Guid.NewGuid();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DashboardTestDbContext>();
            await db.Database.EnsureCreatedAsync();

            db.Applications.Add(new ManagementApplication
            {
                Id = appId,
                ClientId = "tagged-client",
                DisplayName = "Tagged Client App",
                Tags = ["Mobile", "Production"],
                CreatedAt = DateTimeOffset.UtcNow,
                Permissions = "[]"
            });
            await db.SaveChangesAsync();
        }

        app.UseRouting();
        app.MapOpenIddictManagementDashboard(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync($"/management/Applications/Details?id={appId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Tags");
        content.Should().Contain("Mobile");
        content.Should().Contain("Production");

        await app.StopAsync();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private class DashboardTestDbContext(DbContextOptions<DashboardTestDbContext> options) : DbContext(options)
    {
        public DbSet<ManagementApplication> Applications => Set<ManagementApplication>();
        public DbSet<ManagementScope> Scopes => Set<ManagementScope>();
        public DbSet<ManagementToken> Tokens => Set<ManagementToken>();
        public DbSet<ManagementAuthorization> Authorizations => Set<ManagementAuthorization>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.UseOpenIddictManagement();
    }
}
