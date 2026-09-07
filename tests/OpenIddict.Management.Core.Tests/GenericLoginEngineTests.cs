using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Management.Builder;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Extensions;
using OpenIddict.Management.Models;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class GenericLoginEngineTests
{
    public class CustomLoginRequest
    {
        public string EmailAddress { get; set; } = string.Empty;
        public string SecretCode { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
    }

    private class CustomAuthProvider : IUserAuthenticationProvider<CustomLoginRequest>
    {
        public Task<LoginResult> AuthenticateAsync(CustomLoginRequest context, CancellationToken cancellationToken = default)
        {
            if (context.EmailAddress == "dev@corp.com" && context.SecretCode == "magic-code" && context.TenantId == "tenant-1")
            {
                return Task.FromResult(LoginResult.Success(
                    userId: "dev-001",
                    username: "dev@corp.com",
                    roles: ["Developer"],
                    scopes: ["openid", "api_access"],
                    extraData: new { Tenant = context.TenantId }
                ));
            }

            return Task.FromResult(LoginResult.InvalidCredentials("Invalid corporate credentials."));
        }
    }

    [Fact]
    public async Task AuthenticateAsync_WithGenericContext_DelegatesToGenericProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOpenIddictManagement()
            .AddAuthenticationProvider<CustomAuthProvider, CustomLoginRequest>();

        var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IOpenIddictLoginEngine<CustomLoginRequest>>();
        var tokenService = provider.GetRequiredService<IOpenIddictTokenService>();

        var context = new CustomLoginRequest
        {
            EmailAddress = "dev@corp.com",
            SecretCode = "magic-code",
            TenantId = "tenant-1"
        };

        // Act
        var result = await engine.AuthenticateAsync(context);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.UserId.Should().Be("dev-001");

        // Verify token creation from the generic result
        var principal = tokenService.CreatePrincipal(result);
        principal.GetClaim("Tenant").Should().Be("tenant-1");
    }

    [Fact]
    public async Task AuthenticateAsync_WithoutProvider_ReturnsFailedResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOpenIddictManagement();

        var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IOpenIddictLoginEngine>();

        var context = new LoginContext
        {
            Username = "user",
            Password = "password"
        };

        // Act
        var result = await engine.AuthenticateAsync(context);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorHeader.Should().Be("NoAuthenticationProvider");
    }
}
