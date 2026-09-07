using System.Security.Claims;
using FluentAssertions;
using OpenIddict.Abstractions;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Models;
using OpenIddict.Management.Services;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class TokenServiceTests
{
    private readonly OpenIddictTokenService _tokenService = new();

    [Fact]
    public void CreatePrincipal_FromParameters_SetsStandardClaimsAndDestinations()
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
        var principal = _tokenService.CreatePrincipal(parameters);

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
    public void CreatePrincipal_WithIdentityTokenMode_SetsIdentityTokenDestination()
    {
        // Arrange
        var parameters = new TokenCreationParameters
        {
            Subject = "usr-456",
            Username = "janedoe",
            DestinationMode = ClaimDestinationMode.IdentityToken
        };

        // Act
        var principal = _tokenService.CreatePrincipal(parameters);

        // Assert
        var subClaim = principal.Claims.First(c => c.Type == OpenIddictConstants.Claims.Subject);
        subClaim.GetDestinations().Should().ContainSingle().Which.Should().Be(OpenIddictConstants.Destinations.IdentityToken);
    }

    [Fact]
    public void CreatePrincipal_WithBothDestinations_SetsBothDestinations()
    {
        // Arrange
        var parameters = new TokenCreationParameters
        {
            Subject = "usr-789",
            DestinationMode = ClaimDestinationMode.AccessTokenAndIdentityToken
        };

        // Act
        var principal = _tokenService.CreatePrincipal(parameters);

        // Assert
        var subClaim = principal.Claims.First(c => c.Type == OpenIddictConstants.Claims.Subject);
        subClaim.GetDestinations().Should().BeEquivalentTo(new[]
        {
            OpenIddictConstants.Destinations.AccessToken,
            OpenIddictConstants.Destinations.IdentityToken
        });
    }

    [Fact]
    public void CreatePrincipal_WithExtraDataObject_EmbedsPropertiesAsClaims()
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
            ExtraData = extraData,
            DestinationMode = ClaimDestinationMode.AccessToken
        };

        // Act
        var principal = _tokenService.CreatePrincipal(parameters);

        // Assert
        principal.GetClaim("TenantId").Should().Be("tenant-xyz");
        principal.GetClaim("SubscriptionTier").Should().Be("Enterprise");
        principal.GetClaim("IsVip").Should().Be("true");

        var tenantClaim = principal.Claims.First(c => c.Type == "TenantId");
        tenantClaim.GetDestinations().Should().ContainSingle().Which.Should().Be(OpenIddictConstants.Destinations.AccessToken);
    }

    [Fact]
    public void CreatePrincipal_WithExtraDataDictionary_EmbedsEntriesAsClaims()
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
            ExtraData = extraData
        };

        // Act
        var principal = _tokenService.CreatePrincipal(parameters);

        // Assert
        principal.GetClaim("Department").Should().Be("Finance");
        principal.GetClaim("CostCenter").Should().Be("1042");
    }

    [Fact]
    public void CreatePrincipal_FromLoginResult_ConstructsValidPrincipal()
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
        var principal = _tokenService.CreatePrincipal(loginResult);

        // Assert
        principal.GetClaim(OpenIddictConstants.Claims.Subject).Should().Be("usr-result-1");
        principal.GetClaim(OpenIddictConstants.Claims.Name).Should().Be("success_user");
        principal.GetClaim(OpenIddictConstants.Claims.Email).Should().Be("user@corp.com");
        principal.GetClaims(OpenIddictConstants.Claims.Role).Should().ContainSingle().Which.Should().Be("SuperAdmin");
        principal.GetClaim("OrgId").Should().Be("org-100");
    }

    [Fact]
    public void CreatePrincipal_FromFailedLoginResult_ThrowsInvalidOperationException()
    {
        // Arrange
        var failedResult = LoginResult.InvalidCredentials();

        // Act & Assert
        var act = () => _tokenService.CreatePrincipal(failedResult);
        act.Should().Throw<InvalidOperationException>();
    }
}
