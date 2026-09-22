using System.Security.Claims;
using FluentAssertions;
using NSubstitute;
using OpenIddict.Abstractions;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Models;
using OpenIddict.Management.Validation;
using OpenIddict.Server;
using OpenIddict.Validation;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class ApplicationValidationHandlerTests
{
    [Fact]
    public async Task ValidationHandler_ValidAccessTokenPrincipal_DoesNotRejectContext()
    {
        var validator = Substitute.For<IOpenIddictApplicationValidator>();
        validator.ValidateClientIdAsync("client-app", Arg.Any<ApplicationValidationContext>())
            .Returns(ValueTask.FromResult(ApplicationValidationResult.Success()));

        var handler = new OpenIddictApplicationValidationHandler(validator);

        var identity = new ClaimsIdentity("Test");
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.ClientId, "client-app"));
        var principal = new ClaimsPrincipal(identity);

        var transaction = new OpenIddictValidationTransaction();
        var context = new OpenIddictValidationEvents.ProcessAuthenticationContext(transaction)
        {
            AccessTokenPrincipal = principal
        };

        // Act - calls IOpenIddictValidationHandler<ProcessAuthenticationContext>
        await handler.HandleAsync(context);

        // Assert
        context.IsRejected.Should().BeFalse();
        context.Error.Should().BeNull();
    }

    [Fact]
    public async Task ValidationHandler_DisabledApplicationToken_RejectsContext()
    {
        var validator = Substitute.For<IOpenIddictApplicationValidator>();
        validator.ValidateClientIdAsync("disabled-client", Arg.Any<ApplicationValidationContext>())
            .Returns(ValueTask.FromResult(ApplicationValidationResult.Failed(
                OpenIddictConstants.Errors.UnauthorizedClient,
                "The client application is disabled.")));

        var handler = new OpenIddictApplicationValidationHandler(validator);

        var identity = new ClaimsIdentity("Test");
        identity.AddClaim(new Claim("client_id", "disabled-client"));
        var principal = new ClaimsPrincipal(identity);

        var transaction = new OpenIddictValidationTransaction();
        var context = new OpenIddictValidationEvents.ProcessAuthenticationContext(transaction)
        {
            AccessTokenPrincipal = principal
        };

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.IsRejected.Should().BeTrue();
        context.Error.Should().Be(OpenIddictConstants.Errors.UnauthorizedClient);
        context.ErrorDescription.Should().Be("The client application is disabled.");
    }

    [Fact]
    public async Task ValidationHandler_ResolvesClientIdFromAuthorizedPartyClaim()
    {
        var validator = Substitute.For<IOpenIddictApplicationValidator>();
        validator.ValidateClientIdAsync("azp-client", Arg.Any<ApplicationValidationContext>())
            .Returns(ValueTask.FromResult(ApplicationValidationResult.Success()));

        var handler = new OpenIddictApplicationValidationHandler(validator);

        var identity = new ClaimsIdentity("Test");
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.AuthorizedParty, "azp-client"));
        var principal = new ClaimsPrincipal(identity);

        var transaction = new OpenIddictValidationTransaction();
        var context = new OpenIddictValidationEvents.ProcessAuthenticationContext(transaction)
        {
            AccessTokenPrincipal = principal
        };

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.IsRejected.Should().BeFalse();
        await validator.Received(1).ValidateClientIdAsync("azp-client", Arg.Any<ApplicationValidationContext>());
    }

    [Fact]
    public async Task ValidationHandler_AlreadyRejectedContext_IsSkipped()
    {
        var validator = Substitute.For<IOpenIddictApplicationValidator>();
        var handler = new OpenIddictApplicationValidationHandler(validator);

        var transaction = new OpenIddictValidationTransaction();
        var context = new OpenIddictValidationEvents.ProcessAuthenticationContext(transaction);
        context.Reject("existing_error", "Existing description");

        // Act
        await handler.HandleAsync(context);

        // Assert
        await validator.DidNotReceiveWithAnyArgs().ValidateClientIdAsync(default!, default!);
    }

    [Theory]
    [InlineData("authorization_code")]
    [InlineData("client_credentials")]
    [InlineData("password")]
    [InlineData("refresh_token")]
    [InlineData("urn:ietf:params:oauth:grant-type:device_code")]
    public async Task ServerHandler_TokenRequest_AnyFlow_ValidClient_Succeeds(string grantType)
    {
        var validator = Substitute.For<IOpenIddictApplicationValidator>();
        validator.ValidateClientIdAsync("test-client", Arg.Any<ApplicationValidationContext>())
            .Returns(ValueTask.FromResult(ApplicationValidationResult.Success()));

        var handler = new OpenIddictApplicationValidationHandler(validator);

        var transaction = new OpenIddictServerTransaction();
        var request = new OpenIddictRequest { GrantType = grantType, ClientId = "test-client" };
        var context = new OpenIddictServerEvents.ValidateTokenRequestContext(transaction)
        {
            Request = request
        };

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.IsRejected.Should().BeFalse();
        await validator.Received(1).ValidateClientIdAsync("test-client", Arg.Is<ApplicationValidationContext>(c =>
            c.EndpointType == "Token" && c.GrantType == grantType));
    }

    [Fact]
    public async Task ServerHandler_TokenRequest_DisabledClient_RejectsRequest()
    {
        var validator = Substitute.For<IOpenIddictApplicationValidator>();
        validator.ValidateClientIdAsync("disabled-client", Arg.Any<ApplicationValidationContext>())
            .Returns(ValueTask.FromResult(ApplicationValidationResult.Failed(
                OpenIddictConstants.Errors.UnauthorizedClient,
                "The client application is disabled.")));

        var handler = new OpenIddictApplicationValidationHandler(validator);

        var transaction = new OpenIddictServerTransaction();
        var request = new OpenIddictRequest { GrantType = "client_credentials", ClientId = "disabled-client" };
        var context = new OpenIddictServerEvents.ValidateTokenRequestContext(transaction)
        {
            Request = request
        };

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.IsRejected.Should().BeTrue();
        context.Error.Should().Be(OpenIddictConstants.Errors.UnauthorizedClient);
        context.ErrorDescription.Should().Be("The client application is disabled.");
    }

    [Fact]
    public async Task ServerHandler_AuthorizationRequest_ValidClient_Succeeds()
    {
        var validator = Substitute.For<IOpenIddictApplicationValidator>();
        validator.ValidateClientIdAsync("auth-code-client", Arg.Any<ApplicationValidationContext>())
            .Returns(ValueTask.FromResult(ApplicationValidationResult.Success()));

        var handler = new OpenIddictApplicationValidationHandler(validator);

        var transaction = new OpenIddictServerTransaction();
        var request = new OpenIddictRequest { ResponseType = "code", ClientId = "auth-code-client" };
        var context = new OpenIddictServerEvents.ValidateAuthorizationRequestContext(transaction)
        {
            Request = request
        };

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.IsRejected.Should().BeFalse();
        await validator.Received(1).ValidateClientIdAsync("auth-code-client", Arg.Is<ApplicationValidationContext>(c =>
            c.EndpointType == "Authorization"));
    }

    [Fact]
    public async Task ServerHandler_AuthorizationRequest_DisabledClient_RejectsRequest()
    {
        var validator = Substitute.For<IOpenIddictApplicationValidator>();
        validator.ValidateClientIdAsync("disabled-auth-client", Arg.Any<ApplicationValidationContext>())
            .Returns(ValueTask.FromResult(ApplicationValidationResult.Failed(
                OpenIddictConstants.Errors.UnauthorizedClient,
                "Application is not active.")));

        var handler = new OpenIddictApplicationValidationHandler(validator);

        var transaction = new OpenIddictServerTransaction();
        var request = new OpenIddictRequest { ResponseType = "code", ClientId = "disabled-auth-client" };
        var context = new OpenIddictServerEvents.ValidateAuthorizationRequestContext(transaction)
        {
            Request = request
        };

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.IsRejected.Should().BeTrue();
        context.Error.Should().Be(OpenIddictConstants.Errors.UnauthorizedClient);
    }

    [Fact]
    public async Task ServerHandler_DeviceRequest_ValidClient_Succeeds()
    {
        var validator = Substitute.For<IOpenIddictApplicationValidator>();
        validator.ValidateClientIdAsync("device-client", Arg.Any<ApplicationValidationContext>())
            .Returns(ValueTask.FromResult(ApplicationValidationResult.Success()));

        var handler = new OpenIddictApplicationValidationHandler(validator);

        var transaction = new OpenIddictServerTransaction();
        var request = new OpenIddictRequest { ClientId = "device-client" };
        var context = new OpenIddictServerEvents.ValidateDeviceRequestContext(transaction)
        {
            Request = request
        };

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.IsRejected.Should().BeFalse();
        await validator.Received(1).ValidateClientIdAsync("device-client", Arg.Is<ApplicationValidationContext>(c =>
            c.EndpointType == "Device"));
    }

    [Fact]
    public async Task ServerHandler_DeviceRequest_DisabledClient_RejectsRequest()
    {
        var validator = Substitute.For<IOpenIddictApplicationValidator>();
        validator.ValidateClientIdAsync("disabled-device-client", Arg.Any<ApplicationValidationContext>())
            .Returns(ValueTask.FromResult(ApplicationValidationResult.Failed(
                OpenIddictConstants.Errors.UnauthorizedClient,
                "Device client is disabled.")));

        var handler = new OpenIddictApplicationValidationHandler(validator);

        var transaction = new OpenIddictServerTransaction();
        var request = new OpenIddictRequest { ClientId = "disabled-device-client" };
        var context = new OpenIddictServerEvents.ValidateDeviceRequestContext(transaction)
        {
            Request = request
        };

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.IsRejected.Should().BeTrue();
        context.Error.Should().Be(OpenIddictConstants.Errors.UnauthorizedClient);
    }
}
