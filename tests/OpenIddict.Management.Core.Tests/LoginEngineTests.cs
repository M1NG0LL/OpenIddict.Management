using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Management.Builder;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Extensions;
using OpenIddict.Management.Models;
using OpenIddict.Management.Services;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class LoginEngineTests
{
    [Fact]
    public async Task AuthenticateAsync_NullUsernameOrPassword_ReturnsInvalidCredentials()
    {
        // Arrange
        var engine = new OpenIddictLoginEngine();
        var context = new LoginContext
        {
            Username = "",
            Password = "secret-password"
        };

        // Act
        var result = await engine.AuthenticateAsync(context);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Type.Should().Be(LoginResultType.InvalidCredentials);
        result.ErrorHeader.Should().Be("InvalidCredentials");
    }

    [Fact]
    public async Task AuthenticateAsync_WithCustomProvider_DelegatesToProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOpenIddictManagement()
            .AddAuthenticationProvider<TestAuthProvider>();

        var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IOpenIddictLoginEngine>();

        var context = new LoginContext
        {
            Username = "valid-user",
            Password = "valid-password"
        };

        // Act
        var result = await engine.AuthenticateAsync(context);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.UserId.Should().Be("usr-123");
        result.Username.Should().Be("valid-user");
    }

    private class TestAuthProvider : IUserAuthenticationProvider
    {
        public Task<LoginResult> AuthenticateAsync(LoginContext context, CancellationToken cancellationToken = default)
        {
            if (context.Username == "valid-user" && context.Password == "valid-password")
            {
                return Task.FromResult(LoginResult.Success("usr-123", "valid-user"));
            }

            return Task.FromResult(LoginResult.InvalidCredentials());
        }
    }
}
