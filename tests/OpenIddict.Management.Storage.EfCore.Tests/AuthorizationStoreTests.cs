using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Extensions;
using OpenIddict.Management.Storage.EfCore.Stores;
using Xunit;

namespace OpenIddict.Management.Storage.EfCore.Tests;

public class AuthorizationStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TestDbContext> _options;

    public AuthorizationStoreTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new TestDbContext(_options);
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetActiveAuthorizationsCountAsync_ReturnsActiveCount()
    {
        // Arrange
        await using (var context = new TestDbContext(_options))
        {
            context.Authorizations.AddRange(
                new ManagementAuthorization { Id = Guid.NewGuid(), Subject = "u1", Status = "valid", CreationDate = DateTime.UtcNow },
                new ManagementAuthorization { Id = Guid.NewGuid(), Subject = "u2", Status = "valid", CreationDate = DateTime.UtcNow },
                new ManagementAuthorization { Id = Guid.NewGuid(), Subject = "u3", Status = "revoked", CreationDate = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();
        }

        // Act & Assert
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreAuthorizationStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.GetActiveAuthorizationsCountAsync();

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(2);
        }
    }

    [Fact]
    public async Task ListSessionsAsync_ReturnsPaginatedAndFilteredSessions()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var nowOffset = DateTimeOffset.UtcNow;

        await using (var context = new TestDbContext(_options))
        {
            var app = new ManagementApplication
            {
                Id = Guid.NewGuid(),
                ClientId = "session-test-client",
                DisplayName = "Session App",
                CreatedAt = nowOffset
            };
            context.Applications.Add(app);

            var auth1 = new ManagementAuthorization
            {
                Id = Guid.NewGuid(),
                Application = app,
                Subject = "alice",
                Status = "valid",
                Scopes = "openid profile",
                CreationDate = now,
                CreatedAt = nowOffset
            };
            var auth2 = new ManagementAuthorization
            {
                Id = Guid.NewGuid(),
                Application = app,
                Subject = "bob",
                Status = "revoked",
                Scopes = "openid",
                CreationDate = now.AddMinutes(-5),
                CreatedAt = nowOffset.AddMinutes(-5)
            };
            context.Authorizations.AddRange(auth1, auth2);

            context.Tokens.Add(new ManagementToken
            {
                Id = Guid.NewGuid(),
                Authorization = auth1,
                Application = app,
                CreationDate = now,
                Status = "valid"
            });

            await context.SaveChangesAsync();
        }

        // Act & Assert
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreAuthorizationStore<TestDbContext, Guid>(context, TimeProvider.System);

            // Filter active only
            var result = await store.ListSessionsAsync(new SessionFilterRequest
            {
                Status = "active",
                PageIndex = 1,
                PageSize = 10
            });

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.TotalCount.Should().Be(1);
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].Subject.Should().Be("alice");
            result.Value.Items[0].TokenCount.Should().Be(1);
            result.Value.Items[0].IsRevoked.Should().BeFalse();
        }
    }

    [Fact]
    public async Task RevokeSessionAuthorizationsAsync_ByUserId_RevokesMatchingSessionsAndTokens()
    {
        // Arrange
        var userId = "session-user";
        var authId = Guid.NewGuid();

        await using (var context = new TestDbContext(_options))
        {
            var auth = new ManagementAuthorization
            {
                Id = authId,
                Subject = userId,
                Status = "valid",
                CreationDate = DateTime.UtcNow
            };
            context.Authorizations.Add(auth);

            context.Tokens.Add(new ManagementToken
            {
                Id = Guid.NewGuid(),
                Authorization = auth,
                Subject = userId,
                Status = "valid",
                CreationDate = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreAuthorizationStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.RevokeSessionAuthorizationsAsync(userId: userId);

            result.IsSuccess.Should().BeTrue();
            result.Value!.AuthorizationsRevoked.Should().Be(1);
            result.Value.TokensRevoked.Should().Be(1);
            result.Value.Scope.Should().Be(RevocationScope.Session);
        }

        // Verify DB
        await using (var context = new TestDbContext(_options))
        {
            var auth = await context.Authorizations.FindAsync(authId);
            auth!.Status.Should().Be("revoked");

            var tokens = await context.Tokens.Where(t => t.Subject == userId).ToListAsync();
            tokens.Should().AllSatisfy(t => t.Status.Should().Be("revoked"));
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<ManagementApplication> Applications => Set<ManagementApplication>();
        public DbSet<ManagementAuthorization> Authorizations => Set<ManagementAuthorization>();
        public DbSet<ManagementToken> Tokens => Set<ManagementToken>();
        public DbSet<ManagementScope> Scopes => Set<ManagementScope>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseOpenIddictManagement();
        }
    }
}
