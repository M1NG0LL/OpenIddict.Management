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

public class TokenStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TestDbContext> _options;

    public TokenStoreTests()
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
            var store = new EfCoreTokenStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.RevokeByUserAsync(userId);

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
                    CreationDate = DateTime.UtcNow.AddHours(-1)
                },
                new ManagementToken
                {
                    Id = Guid.NewGuid(),
                    Subject = "other-user",
                    Application = app,
                    Status = "valid",
                    Type = "refresh_token",
                    CreationDate = DateTime.UtcNow.AddHours(-2)
                },
                new ManagementToken
                {
                    Id = Guid.NewGuid(),
                    Subject = userId,
                    Status = "revoked",
                    Type = "access_token",
                    CreationDate = DateTime.UtcNow.AddDays(-5)
                }
            );
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreTokenStore<TestDbContext, Guid>(context, TimeProvider.System);

            // Filter by ClientId and UserId
            var result = await store.ListTokensAsync(new TokenFilterRequest
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
                new ManagementToken { Id = Guid.NewGuid(), Application = app, Status = "valid", CreationDate = DateTime.UtcNow },
                new ManagementToken { Id = Guid.NewGuid(), Application = app, Status = "valid", CreationDate = DateTime.UtcNow },
                new ManagementToken { Id = Guid.NewGuid(), Application = otherApp, Status = "valid", CreationDate = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreTokenStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.RevokeTokensWithFilterAsync(new TokenFilterRequest
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
            var store = new EfCoreTokenStore<TestDbContext, Guid>(context, TimeProvider.System);
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
    public async Task ListTokensAsync_WithAuthorizationId_FiltersCorrectly()
    {
        var auth1Id = Guid.NewGuid();
        var auth2Id = Guid.NewGuid();
        var token1Id = Guid.NewGuid();
        var token2Id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using (var context = new TestDbContext(_options))
        {
            var app = new ManagementApplication
            {
                Id = Guid.NewGuid(),
                ClientId = "auth-filter-client",
                CreatedAt = DateTimeOffset.UtcNow
            };
            var auth1 = new ManagementAuthorization
            {
                Id = auth1Id,
                Application = app,
                Subject = "user-1",
                Status = "valid"
            };
            var auth2 = new ManagementAuthorization
            {
                Id = auth2Id,
                Application = app,
                Subject = "user-2",
                Status = "valid"
            };
            context.Applications.Add(app);
            context.Authorizations.AddRange(auth1, auth2);

            context.Tokens.AddRange(
                new ManagementToken
                {
                    Id = token1Id,
                    Application = app,
                    Authorization = auth1,
                    Status = "valid",
                    Type = "access_token",
                    CreationDate = now
                },
                new ManagementToken
                {
                    Id = token2Id,
                    Application = app,
                    Authorization = auth2,
                    Status = "valid",
                    Type = "access_token",
                    CreationDate = now
                }
            );
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreTokenStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.ListTokensAsync(new TokenFilterRequest
            {
                AuthorizationId = auth1Id.ToString()
            });

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            Guid.Parse(result.Value.Items[0].Id).Should().Be(token1Id);
        }
    }

    [Fact]
    public async Task GetTokenCountsAsync_ReturnsAccurateCounts()
    {
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
            var store = new EfCoreTokenStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.GetTokenCountsAsync();

            result.IsSuccess.Should().BeTrue();
            result.Value.Total.Should().Be(4);
            result.Value.Valid.Should().Be(2);
            result.Value.Expired.Should().Be(1);
            result.Value.Revoked.Should().Be(1);
        }
    }

    [Fact]
    public async Task GetTokenCountsByApplicationAsync_ReturnsAppTokenCounts()
    {
        var now = DateTimeOffset.UtcNow;
        await using (var context = new TestDbContext(_options))
        {
            var app1 = new ManagementApplication
            {
                Id = Guid.NewGuid(),
                ClientId = "app-1",
                DisplayName = "App One",
                CreatedAt = now
            };
            var app2 = new ManagementApplication
            {
                Id = Guid.NewGuid(),
                ClientId = "app-2",
                DisplayName = "App Two",
                CreatedAt = now
            };
            context.Applications.AddRange(app1, app2);

            context.Tokens.AddRange(
                new ManagementToken { Id = Guid.NewGuid(), Application = app1, CreationDate = now.UtcDateTime, Status = "valid" },
                new ManagementToken { Id = Guid.NewGuid(), Application = app1, CreationDate = now.UtcDateTime, Status = "valid" },
                new ManagementToken { Id = Guid.NewGuid(), Application = app2, CreationDate = now.UtcDateTime, Status = "valid" }
            );
            await context.SaveChangesAsync();
        }

        // Act & Assert
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreTokenStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.GetTokenCountsByApplicationAsync();

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(2);
            result.Value![0].ClientId.Should().Be("app-1");
            result.Value[0].TokenCount.Should().Be(2);
            result.Value[1].ClientId.Should().Be("app-2");
            result.Value[1].TokenCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task GetTokenTimelineAsync_ReturnsDailyCountsAndFiltersByClient()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        var now = DateTime.UtcNow;

        await using (var context = new TestDbContext(_options))
        {
            var app = new ManagementApplication
            {
                Id = Guid.NewGuid(),
                ClientId = "timeline-app",
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Applications.Add(app);

            context.Tokens.AddRange(
                new ManagementToken { Id = Guid.NewGuid(), Application = app, CreationDate = now, Status = "valid" },
                new ManagementToken { Id = Guid.NewGuid(), Application = app, CreationDate = now.AddDays(-1), Status = "valid" }
            );
            await context.SaveChangesAsync();
        }

        // Act & Assert
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreTokenStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.GetTokenTimelineAsync(yesterday, today, "timeline-app");

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(2);
            result.Value!.First(d => d.Date == yesterday).Count.Should().Be(1);
            result.Value!.First(d => d.Date == today).Count.Should().Be(1);
        }
    }

    [Fact]
    public async Task PruneTokensAsync_RemovesExpiredAndRevokedTokens()
    {
        var expiredTokenId = Guid.NewGuid();
        var revokedTokenId = Guid.NewGuid();
        var validTokenId = Guid.NewGuid();

        await using (var context = new TestDbContext(_options))
        {
            var app = new ManagementApplication
            {
                Id = Guid.NewGuid(),
                ClientId = "prune-test-client",
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Applications.Add(app);

            context.Tokens.AddRange(
                new ManagementToken
                {
                    Id = expiredTokenId,
                    Application = app,
                    Status = "valid",
                    CreationDate = DateTime.UtcNow.AddDays(-2),
                    ExpirationDate = DateTime.UtcNow.AddDays(-1)
                },
                new ManagementToken
                {
                    Id = revokedTokenId,
                    Application = app,
                    Status = "revoked",
                    RevokedAt = DateTimeOffset.UtcNow.AddHours(-1),
                    CreationDate = DateTime.UtcNow.AddDays(-1),
                    ExpirationDate = DateTime.UtcNow.AddDays(1)
                },
                new ManagementToken
                {
                    Id = validTokenId,
                    Application = app,
                    Status = "valid",
                    CreationDate = DateTime.UtcNow,
                    ExpirationDate = DateTime.UtcNow.AddDays(1)
                }
            );
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreTokenStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.PruneTokensAsync(batchSize: null, includeRevoked: true);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(2);

            var remaining = await context.Tokens.ToListAsync();
            remaining.Should().HaveCount(1);
            remaining[0].Id.Should().Be(validTokenId);
        }
    }

    [Fact]
    public async Task PruneTokensAsync_WithBatchSize_LimitsPrunedTokens()
    {
        await using (var context = new TestDbContext(_options))
        {
            var app = new ManagementApplication
            {
                Id = Guid.NewGuid(),
                ClientId = "prune-batch-client",
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Applications.Add(app);

            for (int i = 0; i < 5; i++)
            {
                context.Tokens.Add(new ManagementToken
                {
                    Id = Guid.NewGuid(),
                    Application = app,
                    Status = "valid",
                    CreationDate = DateTime.UtcNow.AddHours(-i - 5),
                    ExpirationDate = DateTime.UtcNow.AddHours(-i - 1)
                });
            }
            await context.SaveChangesAsync();
        }

        // Act - prune with batchSize = 2
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreTokenStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.PruneTokensAsync(batchSize: 2, includeRevoked: true);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(2);

            var remaining = await context.Tokens.CountAsync();
            remaining.Should().Be(3);
        }
    }

    [Fact]
    public async Task PruneTokensAsync_WithoutIncludeRevoked_RemovesOnlyExpiredTokens()
    {
        var expiredTokenId = Guid.NewGuid();
        var revokedTokenId = Guid.NewGuid();

        await using (var context = new TestDbContext(_options))
        {
            var app = new ManagementApplication
            {
                Id = Guid.NewGuid(),
                ClientId = "prune-expired-only-client",
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Applications.Add(app);

            context.Tokens.AddRange(
                new ManagementToken
                {
                    Id = expiredTokenId,
                    Application = app,
                    Status = "valid",
                    CreationDate = DateTime.UtcNow.AddDays(-2),
                    ExpirationDate = DateTime.UtcNow.AddDays(-1)
                },
                new ManagementToken
                {
                    Id = revokedTokenId,
                    Application = app,
                    Status = "revoked",
                    RevokedAt = DateTimeOffset.UtcNow.AddHours(-1),
                    CreationDate = DateTime.UtcNow,
                    ExpirationDate = DateTime.UtcNow.AddDays(1)
                }
            );
            await context.SaveChangesAsync();
        }

        // Act - includeRevoked = false
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreTokenStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.PruneTokensAsync(batchSize: null, includeRevoked: false);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(1);

            var remaining = await context.Tokens.ToListAsync();
            remaining.Should().HaveCount(1);
            remaining[0].Id.Should().Be(revokedTokenId);
        }
    }

    [Fact]
    public async Task ExtendTokenExpirationAsync_ValidAndExpiredTokens_ExtendsBoth()
    {
        var now = DateTime.UtcNow;
        var validTokenId = Guid.NewGuid();
        var expiredTokenId = Guid.NewGuid();
        var revokedTokenId = Guid.NewGuid();

        await using (var context = new TestDbContext(_options))
        {
            context.Tokens.AddRange(
                new ManagementToken
                {
                    Id = validTokenId,
                    Status = "valid",
                    CreationDate = now.AddHours(-1),
                    ExpirationDate = now.AddMinutes(10)
                },
                new ManagementToken
                {
                    Id = expiredTokenId,
                    Status = "expired",
                    CreationDate = now.AddDays(-2),
                    ExpirationDate = now.AddDays(-1)
                },
                new ManagementToken
                {
                    Id = revokedTokenId,
                    Status = "revoked",
                    RevokedAt = now.AddDays(-1),
                    CreationDate = now.AddDays(-3),
                    ExpirationDate = now.AddDays(1)
                }
            );
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreTokenStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.ExtendTokenExpirationAsync(
                new[] { validTokenId.ToString(), expiredTokenId.ToString(), revokedTokenId.ToString() },
                additionalMinutes: 60);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(2);
        }

        // Verify in database
        await using (var context = new TestDbContext(_options))
        {
            var validToken = await context.Tokens.FindAsync(validTokenId);
            validToken.Should().NotBeNull();
            validToken!.ExpirationDate.Should().BeAfter(now.AddMinutes(65));

            var expiredToken = await context.Tokens.FindAsync(expiredTokenId);
            expiredToken.Should().NotBeNull();
            expiredToken!.Status.Should().Be("valid");
            expiredToken.ExpirationDate.Should().BeAfter(now.AddMinutes(55));

            var revokedToken = await context.Tokens.FindAsync(revokedTokenId);
            revokedToken.Should().NotBeNull();
            revokedToken!.Status.Should().Be("revoked");
        }
    }

    [Fact]
    public async Task RevokeMultipleTokensAsync_MultipleValidTokens_RevokesAll()
    {
        var token1 = Guid.NewGuid();
        var token2 = Guid.NewGuid();
        var token3 = Guid.NewGuid();

        await using (var context = new TestDbContext(_options))
        {
            context.Tokens.AddRange(
                new ManagementToken { Id = token1, Status = "valid", CreationDate = DateTime.UtcNow },
                new ManagementToken { Id = token2, Status = "valid", CreationDate = DateTime.UtcNow },
                new ManagementToken { Id = token3, Status = "revoked", RevokedAt = DateTime.UtcNow, CreationDate = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreTokenStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.RevokeMultipleTokensAsync(new[] { token1.ToString(), token2.ToString(), token3.ToString() });

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.TokensRevoked.Should().Be(2);
        }

        // Verify DB
        await using (var context = new TestDbContext(_options))
        {
            var t1 = await context.Tokens.FindAsync(token1);
            var t2 = await context.Tokens.FindAsync(token2);
            t1!.Status.Should().Be("revoked");
            t1.RevokedAt.Should().NotBeNull();
            t2!.Status.Should().Be("revoked");
            t2.RevokedAt.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task IntrospectTokenAsync_WhenTokenExists_ReturnsIntrospectionDetails()
    {
        var tokenId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var app = new ManagementApplication { Id = appId, ClientId = "client-alpha", DisplayName = "Alpha App" };
        await using (var context = new TestDbContext(_options))
        {
            context.Applications.Add(app);
            context.Tokens.Add(new ManagementToken
            {
                Id = tokenId,
                Application = app,
                Subject = "user-alice",
                Type = "access_token",
                Status = "valid",
                CreationDate = now.AddMinutes(-10),
                ExpirationDate = now.AddMinutes(50),
                Payload = "{\"scope\":\"api_read api_write\",\"aud\":\"https://api.example.com\"}"
            });
            await context.SaveChangesAsync();
        }

        await using (var context = new TestDbContext(_options))
        {
            var store = new EfCoreTokenStore<TestDbContext, Guid>(context, TimeProvider.System);
            var result = await store.IntrospectTokenAsync(tokenId.ToString());

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.TokenId.Should().Be(tokenId.ToString());
            result.Value.Active.Should().BeTrue();
            result.Value.Subject.Should().Be("user-alice");
            result.Value.ClientId.Should().Be("client-alpha");
            result.Value.ClientDisplayName.Should().Be("Alpha App");
            result.Value.Claims.Should().ContainKey("scope");
            result.Value.Claims["scope"].Should().Be("api_read api_write");
        }
    }

    [Fact]
    public async Task IntrospectTokenAsync_WhenNotFound_ReturnsFailure()
    {
        await using var context = new TestDbContext(_options);
        var store = new EfCoreTokenStore<TestDbContext, Guid>(context, TimeProvider.System);
        var result = await store.IntrospectTokenAsync(Guid.NewGuid().ToString());

        result.IsSuccess.Should().BeFalse();
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
