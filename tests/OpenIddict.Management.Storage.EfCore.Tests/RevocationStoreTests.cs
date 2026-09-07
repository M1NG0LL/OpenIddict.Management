using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Extensions;
using OpenIddict.Management.Storage.EfCore.Stores;
using Xunit;

namespace OpenIddict.Management.Storage.EfCore.Tests;

public class RevocationStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TestDbContext> _options;

    public RevocationStoreTests()
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
    public async Task RevokeByUserAsync_ValidUserId_RevokesAllUserTokens()
    {
        // Arrange
        var userId = "user-abc-123";
        await using (var context = new TestDbContext(_options))
        {
            context.Tokens.AddRange(
                new ManagementToken { Id = Guid.NewGuid(), Subject = userId, Status = "valid", CreationDate = DateTime.UtcNow },
                new ManagementToken { Id = Guid.NewGuid(), Subject = userId, Status = "valid", CreationDate = DateTime.UtcNow },
                new ManagementToken { Id = Guid.NewGuid(), Subject = "other-user", Status = "valid", CreationDate = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new TestDbContext(_options))
        {
            var revocationManager = new EfCoreRevocationStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await revocationManager.RevokeByUserAsync(userId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.TokensRevoked.Should().Be(2);
            result.Value.Scope.Should().Be(RevocationScope.User);
        }

        // Verify state in database
        await using (var context = new TestDbContext(_options))
        {
            var userTokens = await context.Tokens.Where(t => t.Subject == userId).ToListAsync();
            userTokens.Should().AllSatisfy(t => t.Status.Should().Be("revoked"));
        }
    }

    [Fact]
    public async Task ListTokensAsync_WithFilters_ReturnsFilteredPagedResult()
    {
        // Arrange
        var clientId = "target-client";
        var userId = "target-user";

        await using (var context = new TestDbContext(_options))
        {
            var app = new ManagementApplication
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                DisplayName = "Target App"
            };
            context.Applications.Add(app);

            context.Tokens.AddRange(
                new ManagementToken
                {
                    Id = Guid.NewGuid(),
                    Subject = userId,
                    Application = app,
                    Status = "valid",
                    Type = "access_token",
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
                },
                new ManagementToken
                {
                    Id = Guid.NewGuid(),
                    Subject = "other-user",
                    Application = app,
                    Status = "valid",
                    Type = "refresh_token",
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-2)
                },
                new ManagementToken
                {
                    Id = Guid.NewGuid(),
                    Subject = userId,
                    Status = "revoked",
                    Type = "access_token",
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-5)
                }
            );
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreRevocationStore<TestDbContext, Guid>(context, TimeProvider.System);

            // Filter by ClientId and UserId
            var result = await store.ListTokensAsync(new OpenIddict.Management.Dto.TokenFilterRequest
            {
                ClientId = clientId,
                UserId = userId,
                PageIndex = 1,
                PageSize = 10
            });

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.TotalCount.Should().Be(1);
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].ClientId.Should().Be(clientId);
            result.Value.Items[0].Subject.Should().Be(userId);
        }
    }

    [Fact]
    public async Task RevokeTokensWithFilterAsync_MatchingFilter_RevokesMatchingOnly()
    {
        // Arrange
        var targetClient = "client-to-revoke";

        await using (var context = new TestDbContext(_options))
        {
            var app = new ManagementApplication { Id = Guid.NewGuid(), ClientId = targetClient };
            var otherApp = new ManagementApplication { Id = Guid.NewGuid(), ClientId = "keep-client" };
            context.Applications.AddRange(app, otherApp);

            context.Tokens.AddRange(
                new ManagementToken { Id = Guid.NewGuid(), Application = app, Status = "valid", CreatedAt = DateTimeOffset.UtcNow },
                new ManagementToken { Id = Guid.NewGuid(), Application = app, Status = "valid", CreatedAt = DateTimeOffset.UtcNow },
                new ManagementToken { Id = Guid.NewGuid(), Application = otherApp, Status = "valid", CreatedAt = DateTimeOffset.UtcNow }
            );
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreRevocationStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.RevokeTokensWithFilterAsync(new OpenIddict.Management.Dto.TokenFilterRequest
            {
                ClientId = targetClient
            });

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.TokensRevoked.Should().Be(2);
        }

        // Verify database state
        await using (var context = new TestDbContext(_options))
        {
            var revokedTokens = await context.Tokens.Where(t => t.Application != null && t.Application.ClientId == targetClient).ToListAsync();
            revokedTokens.Should().AllSatisfy(t => t.Status.Should().Be("revoked"));

            var keptTokens = await context.Tokens.Where(t => t.Application != null && t.Application.ClientId == "keep-client").ToListAsync();
            keptTokens.Should().AllSatisfy(t => t.Status.Should().Be("valid"));
        }
    }

    [Fact]
    public async Task ListTokensAsync_TokenExpirationDate_IsNormalizedToUtcAndNotPrematurelyExpired()
    {
        // Arrange: simulate database containing a token expiring in 1 hour in UTC
        // When stored in EF Core without timezone (Kind = Unspecified), it must be interpreted as UTC (+00:00)
        var futureUtc = DateTime.UtcNow.AddHours(1);
        var unspecifiedFutureDate = DateTime.SpecifyKind(futureUtc, DateTimeKind.Unspecified);

        var pastUtc = DateTime.UtcNow.AddHours(-1);
        var unspecifiedPastDate = DateTime.SpecifyKind(pastUtc, DateTimeKind.Unspecified);

        var validTokenId = Guid.NewGuid();
        var expiredTokenId = Guid.NewGuid();

        await using (var context = new TestDbContext(_options))
        {
            var app = new ManagementApplication
            {
                Id = Guid.NewGuid(),
                ClientId = "expiry-test-client",
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Applications.Add(app);

            context.Tokens.AddRange(
                new ManagementToken
                {
                    Id = validTokenId,
                    Application = app,
                    Status = "valid",
                    Type = "access_token",
                    CreationDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                    ExpirationDate = unspecifiedFutureDate,
                    Payload = "serialized-payload-123"
                },
                new ManagementToken
                {
                    Id = expiredTokenId,
                    Application = app,
                    Status = "valid",
                    Type = "access_token",
                    CreationDate = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-2), DateTimeKind.Unspecified),
                    ExpirationDate = unspecifiedPastDate
                }
            );
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreRevocationStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.ListTokensAsync(new TokenFilterRequest
            {
                ClientId = "expiry-test-client"
            });

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(2);

            var validDto = result.Value.Items.First(t => string.Equals(t.Id, validTokenId.ToString(), StringComparison.OrdinalIgnoreCase));
            validDto.ExpirationDate.Should().NotBeNull();
            validDto.ExpirationDate!.Value.Offset.Should().Be(TimeSpan.Zero);
            validDto.IsExpired.Should().BeFalse();
            validDto.Payload.Should().Be("serialized-payload-123");
            validDto.SerializedData.Should().Be("serialized-payload-123");

            var expiredDto = result.Value.Items.First(t => string.Equals(t.Id, expiredTokenId.ToString(), StringComparison.OrdinalIgnoreCase));
            expiredDto.ExpirationDate.Should().NotBeNull();
            expiredDto.ExpirationDate!.Value.Offset.Should().Be(TimeSpan.Zero);
            expiredDto.IsExpired.Should().BeTrue();
            expiredDto.SerializedData.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetTokenCountsAsync_ReturnsAccurateCounts()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var validTokenFutureId = Guid.NewGuid();
        var validTokenNullExpiryId = Guid.NewGuid();
        var expiredTokenId = Guid.NewGuid();
        var revokedTokenId = Guid.NewGuid();

        await using (var context = new TestDbContext(_options))
        {
            var app = new ManagementApplication
            {
                Id = Guid.NewGuid(),
                ClientId = "token-counts-test-client",
                CreatedAt = now
            };
            context.Applications.Add(app);

            context.Tokens.AddRange(
                new ManagementToken
                {
                    Id = validTokenFutureId,
                    Application = app,
                    Status = "valid",
                    Type = "access_token",
                    CreationDate = now.AddHours(-1).UtcDateTime,
                    ExpirationDate = now.AddHours(2).UtcDateTime
                },
                new ManagementToken
                {
                    Id = validTokenNullExpiryId,
                    Application = app,
                    Status = "valid",
                    Type = "access_token",
                    CreationDate = now.AddHours(-1).UtcDateTime,
                    ExpirationDate = null
                },
                new ManagementToken
                {
                    Id = expiredTokenId,
                    Application = app,
                    Status = "valid",
                    Type = "access_token",
                    CreationDate = now.AddHours(-5).UtcDateTime,
                    ExpirationDate = now.AddHours(-1).UtcDateTime
                },
                new ManagementToken
                {
                    Id = revokedTokenId,
                    Application = app,
                    Status = "revoked",
                    Type = "access_token",
                    CreationDate = now.AddHours(-3).UtcDateTime,
                    ExpirationDate = now.AddHours(1).UtcDateTime,
                    RevokedAt = now.AddMinutes(-30)
                }
            );
            await context.SaveChangesAsync();
        }

        // Act & Assert
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreRevocationStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.GetTokenCountsAsync();

            result.IsSuccess.Should().BeTrue();
            result.Value.Total.Should().Be(4);
            result.Value.Valid.Should().Be(2);
            result.Value.Expired.Should().Be(1);
            result.Value.Revoked.Should().Be(1);
        }
    }

    [Fact]
    public async Task GetActiveAuthorizationsCountAsync_ReturnsActiveCount()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await using (var context = new TestDbContext(_options))
        {
            var app = new ManagementApplication
            {
                Id = Guid.NewGuid(),
                ClientId = "auth-counts-test-client",
                CreatedAt = now
            };
            context.Applications.Add(app);

            context.Authorizations.AddRange(
                new ManagementAuthorization
                {
                    Id = Guid.NewGuid(),
                    Application = app,
                    Subject = "user-1",
                    Status = "valid",
                    CreatedAt = now
                },
                new ManagementAuthorization
                {
                    Id = Guid.NewGuid(),
                    Application = app,
                    Subject = "user-2",
                    Status = "revoked",
                    CreatedAt = now
                }
            );
            await context.SaveChangesAsync();
        }

        // Act & Assert
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreRevocationStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.GetActiveAuthorizationsCountAsync();

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(1);
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
