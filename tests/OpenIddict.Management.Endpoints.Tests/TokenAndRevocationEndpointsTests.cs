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

public class TokenAndRevocationEndpointsTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public TokenAndRevocationEndpointsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task RevocationEndpoints_CallsExecuteSuccessfully()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddDbContext<RevokeTestDbContext>(opts => opts.UseSqlite(_connection));
        builder.Services.AddOpenIddictManagementStores<RevokeTestDbContext>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RevokeTestDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        app.MapOpenIddictManagementEndpoints(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        // Revoke by user
        var userRequest = new RevocationByUserRequest { UserId = "user-123" };
        var userResponse = await client.PostAsJsonAsync("/api/management/revocation/by-user", userRequest);
        userResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Revoke by client
        var clientRequest = new RevocationByClientRequest { ClientId = "client-456" };
        var clientResponse = await client.PostAsJsonAsync("/api/management/revocation/by-client", clientRequest);
        clientResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Revoke by session
        var sessionRequest = new RevocationBySessionRequest { UserId = "user-123" };
        var sessionResponse = await client.PostAsJsonAsync("/api/management/revocation/by-session", sessionRequest);
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Prune tokens
        var pruneResponse = await client.PostAsync("/api/management/revocation/prune", null);
        pruneResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Revoke token by id
        var tokenResponse = await client.DeleteAsync($"/api/management/tokens/{Guid.NewGuid()}");
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await app.StopAsync();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private class RevokeTestDbContext(DbContextOptions<RevokeTestDbContext> options) : DbContext(options)
    {
        public DbSet<ManagementToken> Tokens => Set<ManagementToken>();
        public DbSet<ManagementAuthorization> Authorizations => Set<ManagementAuthorization>();
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.UseOpenIddictManagement();
    }
}
