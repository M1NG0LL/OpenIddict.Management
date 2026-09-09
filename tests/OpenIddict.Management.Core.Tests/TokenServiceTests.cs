using System.Security.Claims;
using FluentAssertions;
using NSubstitute;
using OpenIddict.Abstractions;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Models;
using OpenIddict.Management.Results;
using OpenIddict.Management.Services;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class TokenServiceTests
{
    private readonly OpenIddictTokenService _tokenService = new();

    [Fact]
    public async Task CreatePrincipalAsync_FromParameters_SetsStandardClaimsAndDestinations()
    {
        // Arrange
        var parameters = new TokenCreationParameters
        {
            Subject = "usr-123",
            Username = "johndoe",
            Email = "john@example.com",
            Roles = ["Admin", "User"],
            Scopes = ["openid", "profile", "api_access"],
            Resources = ["resource_server_1"],
            DestinationMode = ClaimDestinationMode.AccessToken
        };

        // Act
        var principal = await _tokenService.CreatePrincipalAsync(parameters);

        // Assert
        principal.Should().NotBeNull();
        principal.Identity.Should().NotBeNull();
        principal.Identity!.AuthenticationType.Should().Be("OpenIddict.Server.AspNetCore");

        // Claims
        principal.GetClaim(OpenIddictConstants.Claims.Subject).Should().Be("usr-123");
        principal.GetClaim(OpenIddictConstants.Claims.Name).Should().Be("johndoe");
        principal.GetClaim(OpenIddictConstants.Claims.Email).Should().Be("john@example.com");
        principal.GetClaims(OpenIddictConstants.Claims.Role).Should().BeEquivalentTo(new[] { "Admin", "User" });

        // Scopes & Resources
        principal.GetScopes().Should().BeEquivalentTo(new[] { "openid", "profile", "api_access" });
        principal.GetResources().Should().BeEquivalentTo(new[] { "resource_server_1" });

        // Destination: AccessToken by default
        var subClaim = principal.Claims.First(c => c.Type == OpenIddictConstants.Claims.Subject);
        subClaim.GetDestinations().Should().ContainSingle().Which.Should().Be(OpenIddictConstants.Destinations.AccessToken);
    }

    [Fact]
    public async Task CreatePrincipalAsync_WithIdentityTokenMode_SetsIdentityTokenDestination()
    {
        // Arrange
        var parameters = new TokenCreationParameters
        {
            Subject = "usr-456",
            Username = "janedoe",
            Scopes = ["openid"],
            DestinationMode = ClaimDestinationMode.IdentityToken
        };

        // Act
        var principal = await _tokenService.CreatePrincipalAsync(parameters);

        // Assert
        var subClaim = principal.Claims.First(c => c.Type == OpenIddictConstants.Claims.Subject);
        subClaim.GetDestinations().Should().ContainSingle().Which.Should().Be(OpenIddictConstants.Destinations.IdentityToken);
    }

    [Fact]
    public async Task CreatePrincipalAsync_WithBothDestinations_SetsBothDestinations()
    {
        // Arrange
        var parameters = new TokenCreationParameters
        {
            Subject = "usr-789",
            Scopes = ["openid"],
            DestinationMode = ClaimDestinationMode.AccessTokenAndIdentityToken
        };

        // Act
        var principal = await _tokenService.CreatePrincipalAsync(parameters);

        // Assert
        var subClaim = principal.Claims.First(c => c.Type == OpenIddictConstants.Claims.Subject);
        subClaim.GetDestinations().Should().BeEquivalentTo(new[]
        {
            OpenIddictConstants.Destinations.AccessToken,
            OpenIddictConstants.Destinations.IdentityToken
        });
    }

    [Fact]
    public async Task CreatePrincipalAsync_WithExtraDataObject_EmbedsPropertiesAsClaims()
    {
        // Arrange
        var extraData = new
        {
            TenantId = "tenant-xyz",
            SubscriptionTier = "Enterprise",
            IsVip = true
        };

        var parameters = new TokenCreationParameters
        {
            Subject = "usr-extra",
            Scopes = ["openid"],
            ExtraData = extraData,
            DestinationMode = ClaimDestinationMode.AccessToken
        };

        // Act
        var principal = await _tokenService.CreatePrincipalAsync(parameters);

        // Assert
        principal.GetClaim("TenantId").Should().Be("tenant-xyz");
        principal.GetClaim("SubscriptionTier").Should().Be("Enterprise");
        principal.GetClaim("IsVip").Should().Be("true");

        var tenantClaim = principal.Claims.First(c => c.Type == "TenantId");
        tenantClaim.GetDestinations().Should().ContainSingle().Which.Should().Be(OpenIddictConstants.Destinations.AccessToken);
    }

    [Fact]
    public async Task CreatePrincipalAsync_WithExtraDataDictionary_EmbedsEntriesAsClaims()
    {
        // Arrange
        var extraData = new Dictionary<string, object?>
        {
            ["Department"] = "Finance",
            ["CostCenter"] = 1042
        };

        var parameters = new TokenCreationParameters
        {
            Subject = "usr-dict",
            Scopes = ["openid"],
            ExtraData = extraData
        };

        // Act
        var principal = await _tokenService.CreatePrincipalAsync(parameters);

        // Assert
        principal.GetClaim("Department").Should().Be("Finance");
        principal.GetClaim("CostCenter").Should().Be("1042");
    }

    [Fact]
    public async Task CreatePrincipalAsync_FromLoginResult_ConstructsValidPrincipal()
    {
        // Arrange
        var loginResult = LoginResult.Success(
            userId: "usr-result-1",
            username: "success_user",
            roles: ["SuperAdmin"],
            scopes: ["openid", "api"],
            extraData: new { OrgId = "org-100" },
            destinationMode: ClaimDestinationMode.AccessToken,
            email: "user@corp.com"
        );

        // Act
        var principal = await _tokenService.CreatePrincipalAsync(loginResult);

        // Assert
        principal.GetClaim(OpenIddictConstants.Claims.Subject).Should().Be("usr-result-1");
        principal.GetClaim(OpenIddictConstants.Claims.Name).Should().Be("success_user");
        principal.GetClaim(OpenIddictConstants.Claims.Email).Should().Be("user@corp.com");
        principal.GetClaims(OpenIddictConstants.Claims.Role).Should().ContainSingle().Which.Should().Be("SuperAdmin");
        principal.GetClaim("OrgId").Should().Be("org-100");
    }

    [Fact]
    public async Task CreatePrincipalAsync_FromFailedLoginResult_ThrowsInvalidOperationException()
    {
        // Arrange
        var failedResult = LoginResult.InvalidCredentials();

        // Act & Assert
        var act = () => _tokenService.CreatePrincipalAsync(failedResult);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreatePrincipalAsync_WithDestinationSelector_AppliesCustomDestinationsPerClaim()
    {
        // Arrange
        var parameters = new TokenCreationParameters
        {
            Subject = "usr-custom-dest",
            Username = "alice",
            Email = "alice@example.com",
            Roles = ["Manager"],
            Scopes = ["openid"],
            ExtraData = new { Department = "Security" },
            DestinationMode = ClaimDestinationMode.AccessToken,
            DestinationSelector = claim => claim.Type switch
            {
                OpenIddictConstants.Claims.Subject => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
                OpenIddictConstants.Claims.Name => [OpenIddictConstants.Destinations.IdentityToken],
                OpenIddictConstants.Claims.Email => [OpenIddictConstants.Destinations.IdentityToken],
                OpenIddictConstants.Claims.Role => [OpenIddictConstants.Destinations.AccessToken],
                "Department" => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
                _ => [OpenIddictConstants.Destinations.AccessToken]
            }
        };

        // Act
        var principal = await _tokenService.CreatePrincipalAsync(parameters);

        // Assert
        var subClaim = principal.Claims.First(c => c.Type == OpenIddictConstants.Claims.Subject);
        subClaim.GetDestinations().Should().BeEquivalentTo([OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken]);

        var nameClaim = principal.Claims.First(c => c.Type == OpenIddictConstants.Claims.Name);
        nameClaim.GetDestinations().Should().ContainSingle().Which.Should().Be(OpenIddictConstants.Destinations.IdentityToken);

        var emailClaim = principal.Claims.First(c => c.Type == OpenIddictConstants.Claims.Email);
        emailClaim.GetDestinations().Should().ContainSingle().Which.Should().Be(OpenIddictConstants.Destinations.IdentityToken);

        var roleClaim = principal.Claims.First(c => c.Type == OpenIddictConstants.Claims.Role);
        roleClaim.GetDestinations().Should().ContainSingle().Which.Should().Be(OpenIddictConstants.Destinations.AccessToken);

        var deptClaim = principal.Claims.First(c => c.Type == "Department");
        deptClaim.GetDestinations().Should().BeEquivalentTo([OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken]);
    }

    [Fact]
    public async Task CreatePrincipalAsync_WithDestinationSelectorReturningNull_FallsBackToDestinationMode()
    {
        // Arrange
        var parameters = new TokenCreationParameters
        {
            Subject = "usr-fallback",
            Username = "bob",
            Scopes = ["openid"],
            DestinationMode = ClaimDestinationMode.IdentityToken,
            DestinationSelector = claim => claim.Type == OpenIddictConstants.Claims.Subject
                ? [OpenIddictConstants.Destinations.AccessToken]
                : null // Fallback to IdentityToken from DestinationMode
        };

        // Act
        var principal = await _tokenService.CreatePrincipalAsync(parameters);

        // Assert
        var subClaim = principal.Claims.First(c => c.Type == OpenIddictConstants.Claims.Subject);
        subClaim.GetDestinations().Should().ContainSingle().Which.Should().Be(OpenIddictConstants.Destinations.AccessToken);

        var nameClaim = principal.Claims.First(c => c.Type == OpenIddictConstants.Claims.Name);
        nameClaim.GetDestinations().Should().ContainSingle().Which.Should().Be(OpenIddictConstants.Destinations.IdentityToken);
    }

    [Fact]
    public async Task CreatePrincipalAsync_FromLoginResultWithDestinationSelector_PropagatesSelector()
    {
        // Arrange
        var loginResult = LoginResult.Success(
            userId: "usr-login-dest",
            username: "charlie",
            roles: ["Auditor"],
            scopes: ["openid"],
            destinationSelector: claim => claim.Type == OpenIddictConstants.Claims.Role
                ? [OpenIddictConstants.Destinations.IdentityToken]
                : [OpenIddictConstants.Destinations.AccessToken]
        );

        // Act
        var principal = await _tokenService.CreatePrincipalAsync(loginResult);

        // Assert
        var roleClaim = principal.Claims.First(c => c.Type == OpenIddictConstants.Claims.Role);
        roleClaim.GetDestinations().Should().ContainSingle().Which.Should().Be(OpenIddictConstants.Destinations.IdentityToken);

        var subClaim = principal.Claims.First(c => c.Type == OpenIddictConstants.Claims.Subject);
        subClaim.GetDestinations().Should().ContainSingle().Which.Should().Be(OpenIddictConstants.Destinations.AccessToken);
    }

    [Fact]
    public async Task CreatePrincipalAsync_TrimsAndRemovesDuplicatedScopes()
    {
        // Arrange
        var parameters = new TokenCreationParameters
        {
            Subject = "usr-scopes",
            Scopes = ["  openid  ", "profile   email", "openid", " EMAIL "]
        };

        // Act
        var principal = await _tokenService.CreatePrincipalAsync(parameters);

        // Assert
        principal.GetScopes().Should().BeEquivalentTo(["openid", "profile", "email"]);
    }

    [Fact]
    public async Task CreatePrincipalAsync_MergesDefaultScopesFromParameters()
    {
        // Arrange
        var parameters = new TokenCreationParameters
        {
            Subject = "usr-scopes-default",
            Scopes = ["openid"],
            DefaultScopes = ["profile", "email", " openid "]
        };

        // Act
        var principal = await _tokenService.CreatePrincipalAsync(parameters);

        // Assert
        principal.GetScopes().Should().BeEquivalentTo(["openid", "profile", "email"]);
    }

    [Fact]
    public async Task CreatePrincipalAsync_WithEmptyScopesAndEmptyDefaultScopes_ThrowsInvalidOperationException()
    {
        // Arrange
        var parameters = new TokenCreationParameters
        {
            Subject = "usr-no-scopes",
            Scopes = []
        };

        // Act & Assert
        var act = () => _tokenService.CreatePrincipalAsync(parameters);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Scopes cannot be empty*");
    }

    [Fact]
    public async Task CreatePrincipalAsync_WithEmptyScopesAndWhitespaceOnly_ThrowsInvalidOperationException()
    {
        // Arrange
        var parameters = new TokenCreationParameters
        {
            Subject = "usr-whitespace-scopes",
            Scopes = ["   ", ""]
        };

        // Act & Assert
        var act = () => _tokenService.CreatePrincipalAsync(parameters);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Scopes cannot be empty*");
    }

    [Fact]
    public async Task CreatePrincipalAsync_WithEmptyScopesAndProvidedDefaultScopes_UsesDefaultScopes()
    {
        // Arrange
        var parameters = new TokenCreationParameters
        {
            Subject = "usr-default-only",
            Scopes = [],
            DefaultScopes = ["openid", "profile"]
        };

        // Act
        var principal = await _tokenService.CreatePrincipalAsync(parameters);

        // Assert
        principal.GetScopes().Should().BeEquivalentTo(["openid", "profile"]);
    }

    [Fact]
    public async Task CreatePrincipalAsync_WithEmptyScopesAndClientId_ResolvesClientDefaultScopes()
    {
        // Arrange
        var appService = Substitute.For<IApplicationManagementService>();
        var clientApp = new ManagedApplication
        {
            Id = "app-default-client",
            ClientId = "client-default-scopes",
            CreatedAt = DateTimeOffset.UtcNow,
            DefaultScopes = ["openid", "profile", "email"]
        };
        appService.GetByClientIdAsync("client-default-scopes", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(clientApp)));

        var tokenService = new OpenIddictTokenService(appService);
        var parameters = new TokenCreationParameters
        {
            Subject = "usr-client-defaults",
            ClientId = "client-default-scopes",
            Scopes = [] // Empty scopes - will be populated by ClientId default scopes
        };

        // Act
        var principal = await tokenService.CreatePrincipalAsync(parameters);

        // Assert
        principal.GetScopes().Should().BeEquivalentTo(["openid", "profile", "email"]);
    }

    [Fact]
    public async Task CreatePrincipalAsync_WithEmptyScopesAndUnknownClientId_ThrowsInvalidOperationException()
    {
        // Arrange
        var appService = Substitute.For<IApplicationManagementService>();
        appService.GetByClientIdAsync("unknown-client", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<ManagedApplication>("NotFound", "Not found")));

        var tokenService = new OpenIddictTokenService(appService);
        var parameters = new TokenCreationParameters
        {
            Subject = "usr-unknown-client",
            ClientId = "unknown-client",
            Scopes = [] // Empty scopes, and client does not exist
        };

        // Act & Assert
        var act = () => tokenService.CreatePrincipalAsync(parameters);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Scopes cannot be empty*");
    }

    [Fact]
    public async Task CreatePrincipalAsync_WithApplicationService_ResolvesAndMergesClientDefaultScopes()
    {
        // Arrange
        var appService = Substitute.For<IApplicationManagementService>();
        var clientApp = new ManagedApplication
        {
            Id = "app-1",
            ClientId = "client-app-1",
            CreatedAt = DateTimeOffset.UtcNow,
            DefaultScopes = ["offline_access", "email"]
        };
        appService.GetByClientIdAsync("client-app-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(clientApp)));

        var tokenService = new OpenIddictTokenService(appService);
        var parameters = new TokenCreationParameters
        {
            Subject = "usr-client-scopes",
            ClientId = "client-app-1",
            Scopes = ["openid", "offline_access"]
        };

        // Act
        var principal = await tokenService.CreatePrincipalAsync(parameters);

        // Assert
        principal.GetScopes().Should().BeEquivalentTo(["openid", "offline_access", "email"]);
    }

    [Fact]
    public async Task CreatePrincipalAsync_FromLoginResult_PropagatesClientIdAndDefaultScopes()
    {
        // Arrange
        var appService = Substitute.For<IApplicationManagementService>();
        var clientApp = new ManagedApplication
        {
            Id = "app-3",
            ClientId = "client-login",
            CreatedAt = DateTimeOffset.UtcNow,
            DefaultScopes = ["roles"]
        };
        appService.GetByClientIdAsync("client-login", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(clientApp)));

        var tokenService = new OpenIddictTokenService(appService);
        var loginResult = LoginResult.Success(
            userId: "usr-result-scopes",
            username: "user_scopes",
            scopes: ["openid", "profile"],
            defaultScopes: ["email", "openid"],
            clientId: "client-login"
        );

        // Act
        var principal = await tokenService.CreatePrincipalAsync(loginResult);

        // Assert
        principal.GetScopes().Should().BeEquivalentTo(["openid", "profile", "email", "roles"]);
    }
}
