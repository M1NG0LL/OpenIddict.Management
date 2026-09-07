using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Management.Builder;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Models;
using OpenIddict.Management.Validation;
using OpenIddict.Server;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class PkceValidationTests
{
    private class DummyAuthProvider : IUserAuthenticationProvider
    {
        public Task<LoginResult> AuthenticateAsync(LoginContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(LoginResult.Success("1", "admin"));
    }

    [Fact]
    public void Validate_ValidConfiguration_Succeeds()
    {
        // Arrange
        var validator = new OpenIddictAuthorizationCodePkceValidator();
        var options = new OpenIddictServerOptions
        {
            RequireProofKeyForCodeExchange = true
        };
        options.GrantTypes.Add(OpenIddictConstants.GrantTypes.AuthorizationCode);
        options.AuthorizationEndpointUris.Add(new Uri("/connect/authorize", UriKind.Relative));

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Failed.Should().BeFalse();
    }

    [Fact]
    public void Validate_MissingAuthorizationCodeFlow_Fails()
    {
        // Arrange
        var validator = new OpenIddictAuthorizationCodePkceValidator();
        var options = new OpenIddictServerOptions
        {
            RequireProofKeyForCodeExchange = true
        };
        options.AuthorizationEndpointUris.Add(new Uri("/connect/authorize", UriKind.Relative));

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Authorization Code Flow");
    }

    [Fact]
    public void Validate_MissingPkce_Fails()
    {
        // Arrange
        var validator = new OpenIddictAuthorizationCodePkceValidator();
        var options = new OpenIddictServerOptions
        {
            RequireProofKeyForCodeExchange = false
        };
        options.GrantTypes.Add(OpenIddictConstants.GrantTypes.AuthorizationCode);
        options.AuthorizationEndpointUris.Add(new Uri("/connect/authorize", UriKind.Relative));

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Proof Key for Code Exchange (PKCE)");
    }

    [Fact]
    public void Validate_MissingAuthorizationEndpoint_Fails()
    {
        // Arrange
        var validator = new OpenIddictAuthorizationCodePkceValidator();
        var options = new OpenIddictServerOptions
        {
            RequireProofKeyForCodeExchange = true
        };
        options.GrantTypes.Add(OpenIddictConstants.GrantTypes.AuthorizationCode);

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("authorization endpoint URI");
    }

    [Fact]
    public void AddAuthenticationProvider_RegistersValidatorInDependencyInjection()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new OpenIddictManagementBuilder(services);

        // Act
        builder.AddAuthenticationProvider<DummyAuthProvider>();

        // Assert
        var provider = services.BuildServiceProvider();
        var validators = provider.GetServices<IValidateOptions<OpenIddictServerOptions>>();
        validators.Should().ContainSingle(v => v is OpenIddictAuthorizationCodePkceValidator);
    }
}
