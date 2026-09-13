using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Endpoints.Extensions;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Extensions;
using Xunit;

namespace OpenIddict.Management.Endpoints.Tests;

public class SessionAndOverviewEndpointsTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public SessionAndOverviewEndpointsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task OverviewAndSessionEndpoints_ReturnSuccessfulResults()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddDbContext<SessionOverviewDbContext>(opts => opts.UseSqlite(_connection));
        builder.Services.AddOpenIddictManagementStores<SessionOverviewDbContext>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SessionOverviewDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        app.MapOpenIddictManagementEndpoints(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        // 1. Overview endpoint
        var overviewResponse = await client.GetAsync("/api/management/overview");
        overviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var overview = await overviewResponse.Content.ReadFromJsonAsync<DashboardOverviewDto>();
        overview.Should().NotBeNull();
        overview!.TotalApplications.Should().Be(0);
        overview.ActiveApplications.Should().Be(0);

        // 2. Sessions endpoint
        var sessionsResponse = await client.GetAsync("/api/management/sessions?pageIndex=1&pageSize=10");
        sessionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var sessionsResult = await sessionsResponse.Content.ReadFromJsonAsync<PagedResult<SessionListDto>>();
        sessionsResult.Should().NotBeNull();
        sessionsResult!.Items.Should().BeEmpty();

        await app.StopAsync();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private class SessionOverviewDbContext(DbContextOptions<SessionOverviewDbContext> options) : DbContext(options)
    {
        public DbSet<ManagementApplication> Applications => Set<ManagementApplication>();
        public DbSet<ManagementScope> Scopes => Set<ManagementScope>();
        public DbSet<ManagementToken> Tokens => Set<ManagementToken>();
        public DbSet<ManagementAuthorization> Authorizations => Set<ManagementAuthorization>();
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.UseOpenIddictManagement();
    }
}
