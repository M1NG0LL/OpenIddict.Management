using FluentAssertions;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;
using NSubstitute;
using OpenIddict.Abstractions;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Models;
using OpenIddict.Management.Options;
using OpenIddict.Management.Results;
using OpenIddict.Management.Validation;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class ApplicationValidatorTests
{
    private static ManagedApplication CreateApp(
        string clientId = "client-123",
        ApplicationStatus status = ApplicationStatus.Active,
        ApplicationEnvironment env = ApplicationEnvironment.Production)
    {
        return new ManagedApplication
        {
            Id = "app-id-1",
            ClientId = clientId,
            DisplayName = "Test App",
            Status = status,
            Environment = env,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public async Task ValidateAsync_ActiveApp_DefaultOptions_Succeeds()
    {
        var options = new OpenIddictApplicationValidationOptions();
        var validator = new OpenIddictApplicationValidator(MicrosoftOptions.Create(options));
        var app = CreateApp();
        var context = new ApplicationValidationContext { ClientId = app.ClientId, Application = app };

        var result = await validator.ValidateAsync(app, context);

        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_DisabledApp_DefaultOptions_FailsWithStatusError()
    {
        var options = new OpenIddictApplicationValidationOptions();
        var validator = new OpenIddictApplicationValidator(MicrosoftOptions.Create(options));
        var app = CreateApp(status: ApplicationStatus.Disabled);
        var context = new ApplicationValidationContext { ClientId = app.ClientId, Application = app };

        var result = await validator.ValidateAsync(app, context);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(OpenIddictConstants.Errors.UnauthorizedClient);
        result.ErrorDescription.Should().Be(options.StatusErrorDescription);
    }

    [Fact]
    public async Task ValidateAsync_DeletedApp_DefaultOptions_FailsWithStatusError()
    {
        var options = new OpenIddictApplicationValidationOptions();
        var validator = new OpenIddictApplicationValidator(MicrosoftOptions.Create(options));
        var app = CreateApp(status: ApplicationStatus.Deleted);
        var context = new ApplicationValidationContext { ClientId = app.ClientId, Application = app };

        var result = await validator.ValidateAsync(app, context);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(OpenIddictConstants.Errors.UnauthorizedClient);
    }

    [Fact]
    public async Task ValidateAsync_StatusValidationDisabled_DisabledApp_Succeeds()
    {
        var options = new OpenIddictApplicationValidationOptions
        {
            ValidateStatus = false
        };
        var validator = new OpenIddictApplicationValidator(MicrosoftOptions.Create(options));
        var app = CreateApp(status: ApplicationStatus.Disabled);
        var context = new ApplicationValidationContext { ClientId = app.ClientId, Application = app };

        var result = await validator.ValidateAsync(app, context);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_EnvironmentValidation_ExpectedEnvironmentMismatch_Fails()
    {
        var options = new OpenIddictApplicationValidationOptions();
        options.RequireEnvironment(ApplicationEnvironment.Production);
        var validator = new OpenIddictApplicationValidator(MicrosoftOptions.Create(options));
        var app = CreateApp(env: ApplicationEnvironment.Development);
        var context = new ApplicationValidationContext { ClientId = app.ClientId, Application = app };

        var result = await validator.ValidateAsync(app, context);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(OpenIddictConstants.Errors.UnauthorizedClient);
        result.ErrorDescription.Should().Be(options.EnvironmentErrorDescription);
    }

    [Fact]
    public async Task ValidateAsync_EnvironmentValidation_ExpectedEnvironmentMatch_Succeeds()
    {
        var options = new OpenIddictApplicationValidationOptions();
        options.RequireEnvironment(ApplicationEnvironment.Production);
        var validator = new OpenIddictApplicationValidator(MicrosoftOptions.Create(options));
        var app = CreateApp(env: ApplicationEnvironment.Production);
        var context = new ApplicationValidationContext { ClientId = app.ClientId, Application = app };

        var result = await validator.ValidateAsync(app, context);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_EnvironmentValidation_AllowedEnvironmentsMismatch_Fails()
    {
        var options = new OpenIddictApplicationValidationOptions();
        options.RequireEnvironments(ApplicationEnvironment.Staging, ApplicationEnvironment.Production);
        var validator = new OpenIddictApplicationValidator(MicrosoftOptions.Create(options));
        var app = CreateApp(env: ApplicationEnvironment.Development);
        var context = new ApplicationValidationContext { ClientId = app.ClientId, Application = app };

        var result = await validator.ValidateAsync(app, context);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(OpenIddictConstants.Errors.UnauthorizedClient);
    }

    [Fact]
    public async Task ValidateAsync_CustomValidator_Fails_ReturnsCustomError()
    {
        var options = new OpenIddictApplicationValidationOptions();
        options.ValidateCustom((app, ctx) =>
            ApplicationValidationResult.Failed("custom_error", "Custom error reason"));

        var validator = new OpenIddictApplicationValidator(MicrosoftOptions.Create(options));
        var app = CreateApp();
        var context = new ApplicationValidationContext { ClientId = app.ClientId, Application = app };

        var result = await validator.ValidateAsync(app, context);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("custom_error");
        result.ErrorDescription.Should().Be("Custom error reason");
    }

    [Fact]
    public async Task ValidateAsync_ValidationGloballyDisabled_AlwaysSucceeds()
    {
        var options = new OpenIddictApplicationValidationOptions
        {
            EnableValidation = false,
            ValidateStatus = true
        };
        var validator = new OpenIddictApplicationValidator(MicrosoftOptions.Create(options));
        var app = CreateApp(status: ApplicationStatus.Disabled);
        var context = new ApplicationValidationContext { ClientId = app.ClientId, Application = app };

        var result = await validator.ValidateAsync(app, context);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateClientIdAsync_ResolvesFromService_AndValidates()
    {
        var appService = Substitute.For<IApplicationManagementService>();
        var app = CreateApp(status: ApplicationStatus.Active);
        appService.GetByClientIdAsync(app.ClientId, Arg.Any<CancellationToken>())
            .Returns(Result<ManagedApplication>.Success(app));

        var options = new OpenIddictApplicationValidationOptions();
        var validator = new OpenIddictApplicationValidator(MicrosoftOptions.Create(options), appService);

        var result = await validator.ValidateClientIdAsync(app.ClientId, new ApplicationValidationContext());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateClientIdAsync_AppNotFound_FailsWithNotFoundError()
    {
        var appService = Substitute.For<IApplicationManagementService>();
        appService.GetByClientIdAsync("unknown-client", Arg.Any<CancellationToken>())
            .Returns(Result<ManagedApplication>.Failure(ManagementError.EntityNotFound("Application", "unknown-client")));

        var options = new OpenIddictApplicationValidationOptions
        {
            RejectWhenApplicationNotFound = true
        };
        var validator = new OpenIddictApplicationValidator(MicrosoftOptions.Create(options), appService);

        var result = await validator.ValidateClientIdAsync("unknown-client", new ApplicationValidationContext());

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(OpenIddictConstants.Errors.InvalidClient);
        result.ErrorDescription.Should().Be(options.NotFoundErrorDescription);
    }

    [Fact]
    public async Task ValidateClientIdAsync_AppNotFound_RejectWhenNotFoundDisabled_Succeeds()
    {
        var appService = Substitute.For<IApplicationManagementService>();
        appService.GetByClientIdAsync("unknown-client", Arg.Any<CancellationToken>())
            .Returns(Result<ManagedApplication>.Failure(ManagementError.EntityNotFound("Application", "unknown-client")));

        var options = new OpenIddictApplicationValidationOptions
        {
            RejectWhenApplicationNotFound = false
        };
        var validator = new OpenIddictApplicationValidator(MicrosoftOptions.Create(options), appService);

        var result = await validator.ValidateClientIdAsync("unknown-client", new ApplicationValidationContext());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateClientIdAsync_EmptyClientId_ReturnsSuccess()
    {
        var options = new OpenIddictApplicationValidationOptions();
        var validator = new OpenIddictApplicationValidator(MicrosoftOptions.Create(options));

        var result = await validator.ValidateClientIdAsync(string.Empty, new ApplicationValidationContext());

        result.Succeeded.Should().BeTrue();
    }
}
