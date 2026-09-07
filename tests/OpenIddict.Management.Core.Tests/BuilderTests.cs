using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Management.Builder;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Models;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class BuilderTests
{
    private sealed class CustomAuthProvider : IUserAuthenticationProvider
    {
        public Task<LoginResult> AuthenticateAsync(LoginContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(LoginResult.Success("custom_user", "custom_name"));
    }

    private sealed class CustomLoginEngine : IOpenIddictLoginEngine
    {
        public Task<LoginResult> AuthenticateAsync(LoginContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(LoginResult.Success("engine_user", "engine_name"));
    }

    private sealed class CustomTokenService : IOpenIddictTokenService
    {
        public ClaimsPrincipal CreatePrincipal(TokenCreationParameters parameters) => new();
        public ClaimsPrincipal CreatePrincipal(LoginResult loginResult) => new();
    }

    [Fact]
    public void AddAuthenticationProvider_RegistersCustomProvider()
    {
        var services = new ServiceCollection();
        var builder = new OpenIddictManagementBuilder(services);

        builder.AddAuthenticationProvider<CustomAuthProvider>();

        var provider = services.BuildServiceProvider();
        var registered = provider.GetService<IUserAuthenticationProvider>();

        registered.Should().NotBeNull();
        registered.Should().BeOfType<CustomAuthProvider>();
    }

    [Fact]
    public void AddLoginEngine_RegistersCustomEngine()
    {
        var services = new ServiceCollection();
        var builder = new OpenIddictManagementBuilder(services);

        builder.AddLoginEngine<CustomLoginEngine>();

        var provider = services.BuildServiceProvider();
        var registered = provider.GetService<IOpenIddictLoginEngine>();

        registered.Should().NotBeNull();
        registered.Should().BeOfType<CustomLoginEngine>();
    }

    [Fact]
    public void AddTokenService_RegistersCustomTokenService()
    {
        var services = new ServiceCollection();
        var builder = new OpenIddictManagementBuilder(services);

        builder.AddTokenService<CustomTokenService>();

        var provider = services.BuildServiceProvider();
        var registered = provider.GetService<IOpenIddictTokenService>();

        registered.Should().NotBeNull();
        registered.Should().BeOfType<CustomTokenService>();
    }
}
