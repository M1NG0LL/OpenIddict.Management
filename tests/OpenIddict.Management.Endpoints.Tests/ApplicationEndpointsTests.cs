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

public class ApplicationEndpointsTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public ApplicationEndpointsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task ApplicationEndpoints_CrudFlow_ReturnsExpectedHttpStatuses()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddDbContext<TestDbContext>(opts => opts.UseSqlite(_connection));
        builder.Services.AddOpenIddictManagementStores<TestDbContext>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        app.MapOpenIddictManagementEndpoints(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        // 1. Create Application
        var createDto = new ApplicationCreateDto
        {
            ClientId = "api-test-client",
            DisplayName = "API Test Application",
            ExtraData = "custom-api-metadata",
            Tags = ["ApiTag1", "SharedTag"]
        };

        var createResponse = await client.PostAsJsonAsync("/api/management/applications", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. Get Application Details
        var listResponse = await client.GetAsync("/api/management/applications?tags=ApiTag1");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResult = await listResponse.Content.ReadFromJsonAsync<PagedResult<ApplicationListDto>>();
        pagedResult.Should().NotBeNull();
        pagedResult!.Items.Should().ContainSingle(a => a.ClientId == "api-test-client");
        pagedResult.Items[0].Tags.Should().Contain("ApiTag1");

        // 3. Get By ID
        var createdAppId = pagedResult.Items[0].Id;
        var getResponse = await client.GetAsync($"/api/management/applications/{createdAppId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var appDetails = await getResponse.Content.ReadFromJsonAsync<ManagedApplication>();
        appDetails.Should().NotBeNull();
        appDetails!.ExtraData.Should().Be("custom-api-metadata");
        appDetails.Tags.Should().BeEquivalentTo(["ApiTag1", "SharedTag"]);
        appDetails.HasTag("ApiTag1").Should().BeTrue();

        await app.StopAsync();
    }

    [Fact]
    public async Task CreateApplication_WithInvalidRedirectUri_ReturnsBadRequestWithValidationDetails()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddDbContext<TestDbContext>(opts => opts.UseSqlite(_connection));
        builder.Services.AddOpenIddictManagementStores<TestDbContext>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        app.MapOpenIddictManagementEndpoints(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        // Act - provide relative URI instead of absolute URI
        var invalidDto = new ApplicationCreateDto
        {
            ClientId = "invalid-uri-client",
            DisplayName = "Invalid URI App",
            RedirectUris = ["/callback"]
        };

        var response = await client.PostAsJsonAsync("/api/management/applications", invalidDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ValidationFailed");
        content.Should().Contain("RedirectUris");

        await app.StopAsync();
    }

    [Fact]
    public async Task CreateApplication_WithWhitespaceClientId_ReturnsBadRequest()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddDbContext<TestDbContext>(opts => opts.UseSqlite(_connection));
        builder.Services.AddOpenIddictManagementStores<TestDbContext>();

        var app = builder.Build();
        app.MapOpenIddictManagementEndpoints(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        var invalidDto = new ApplicationCreateDto
        {
            ClientId = "client with spaces",
            DisplayName = "Spaces Client"
        };

        var response = await client.PostAsJsonAsync("/api/management/applications", invalidDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ClientId");
        content.Should().Contain("cannot contain whitespace");

        await app.StopAsync();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<ManagementApplication> Applications => Set<ManagementApplication>();
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.UseOpenIddictManagement();
    }
}
