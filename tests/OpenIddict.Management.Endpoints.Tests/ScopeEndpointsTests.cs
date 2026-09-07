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
using OpenIddict.Management.Models;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Extensions;
using Xunit;

namespace OpenIddict.Management.Endpoints.Tests;

public class ScopeEndpointsTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public ScopeEndpointsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task ScopeEndpoints_CrudFlow_ReturnsExpectedHttpStatuses()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddDbContext<ScopeTestDbContext>(opts => opts.UseSqlite(_connection));
        builder.Services.AddOpenIddictManagementStores<ScopeTestDbContext>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ScopeTestDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        app.MapOpenIddictManagementEndpoints(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        // 1. Create Scope
        var createRequest = new CreateScopeRequest
        {
            Name = "test_scope",
            DisplayName = "Test Scope",
            Description = "Description",
            Resources = ["rs_api"]
        };
        var createResponse = await client.PostAsJsonAsync("/api/management/scopes", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdScope = await createResponse.Content.ReadFromJsonAsync<ManagedScope>();
        createdScope.Should().NotBeNull();
        createdScope!.Name.Should().Be("test_scope");

        // 2. Get Scope by ID
        var getResponse = await client.GetAsync($"/api/management/scopes/{createdScope.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Update Scope
        var updateRequest = new UpdateScopeRequest
        {
            DisplayName = "Updated Display",
            Description = "Updated Desc",
            Resources = ["rs_api_v2"]
        };
        var updateResponse = await client.PutAsJsonAsync($"/api/management/scopes/{createdScope.Id}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. List Scopes
        var listResponse = await client.GetAsync("/api/management/scopes");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedResult = await listResponse.Content.ReadFromJsonAsync<PagedResult<ManagedScope>>();
        pagedResult.Should().NotBeNull();
        pagedResult!.Items.Should().ContainSingle(s => s.Name == "test_scope");

        // 5. Delete Scope
        var deleteResponse = await client.DeleteAsync($"/api/management/scopes/{createdScope.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await app.StopAsync();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private class ScopeTestDbContext(DbContextOptions<ScopeTestDbContext> options) : DbContext(options)
    {
        public DbSet<ManagementScope> Scopes => Set<ManagementScope>();
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.UseOpenIddictManagement();
    }
}
