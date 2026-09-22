using FluentAssertions;
using OpenIddict.Abstractions;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Options;
using OpenIddict.Management.Validation;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class ApplicationValidationOptionsTests
{
    [Fact]
    public void Options_DefaultValues_AreConfiguredProperly()
    {
        var options = new OpenIddictApplicationValidationOptions();

        options.EnableValidation.Should().BeTrue();
        options.ValidateStatus.Should().BeTrue();
        options.AllowedStatuses.Should().ContainSingle().Which.Should().Be(ApplicationStatus.Active);
        options.ValidateEnvironment.Should().BeFalse();
        options.AllowedEnvironments.Should().BeEquivalentTo(new[]
        {
            ApplicationEnvironment.Development,
            ApplicationEnvironment.Staging,
            ApplicationEnvironment.Production
        });
        options.ExpectedEnvironment.Should().BeNull();
        options.RejectWhenApplicationNotFound.Should().BeTrue();
        options.StatusErrorCode.Should().Be(OpenIddictConstants.Errors.UnauthorizedClient);
        options.StatusErrorDescription.Should().NotBeNullOrWhiteSpace();
        options.EnvironmentErrorCode.Should().Be(OpenIddictConstants.Errors.UnauthorizedClient);
        options.EnvironmentErrorDescription.Should().NotBeNullOrWhiteSpace();
        options.NotFoundErrorCode.Should().Be(OpenIddictConstants.Errors.InvalidClient);
        options.NotFoundErrorDescription.Should().NotBeNullOrWhiteSpace();
        options.CustomValidator.Should().BeNull();
    }

    [Fact]
    public void RequireStatus_SetsAllowedStatusesAndEnablesStatusValidation()
    {
        var options = new OpenIddictApplicationValidationOptions
        {
            ValidateStatus = false
        };

        options.RequireStatus(ApplicationStatus.Active, ApplicationStatus.Disabled);

        options.ValidateStatus.Should().BeTrue();
        options.AllowedStatuses.Should().BeEquivalentTo(new[]
        {
            ApplicationStatus.Active,
            ApplicationStatus.Disabled
        });
    }

    [Fact]
    public void RequireEnvironment_SetsExpectedEnvironmentAndEnablesEnvironmentValidation()
    {
        var options = new OpenIddictApplicationValidationOptions();

        options.RequireEnvironment(ApplicationEnvironment.Production);

        options.ValidateEnvironment.Should().BeTrue();
        options.ExpectedEnvironment.Should().Be(ApplicationEnvironment.Production);
        options.AllowedEnvironments.Should().ContainSingle().Which.Should().Be(ApplicationEnvironment.Production);
    }

    [Fact]
    public void RequireEnvironments_SetsAllowedEnvironmentsAndClearsExpectedEnvironment()
    {
        var options = new OpenIddictApplicationValidationOptions
        {
            ExpectedEnvironment = ApplicationEnvironment.Development
        };

        options.RequireEnvironments(ApplicationEnvironment.Staging, ApplicationEnvironment.Production);

        options.ValidateEnvironment.Should().BeTrue();
        options.ExpectedEnvironment.Should().BeNull();
        options.AllowedEnvironments.Should().BeEquivalentTo(new[]
        {
            ApplicationEnvironment.Staging,
            ApplicationEnvironment.Production
        });
    }

    [Fact]
    public async Task ValidateCustom_AsyncDelegate_IsInvoked()
    {
        var options = new OpenIddictApplicationValidationOptions();
        var wasCalled = false;

        options.ValidateCustom((app, ctx) =>
        {
            wasCalled = true;
            return ValueTask.FromResult(ApplicationValidationResult.Success());
        });

        options.CustomValidator.Should().NotBeNull();
        var result = await options.CustomValidator!(null!, new ApplicationValidationContext());
        wasCalled.Should().BeTrue();
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateCustom_SyncDelegate_IsInvoked()
    {
        var options = new OpenIddictApplicationValidationOptions();
        var wasCalled = false;

        options.ValidateCustom((app, ctx) =>
        {
            wasCalled = true;
            return ApplicationValidationResult.Success();
        });

        options.CustomValidator.Should().NotBeNull();
        var result = await options.CustomValidator!(null!, new ApplicationValidationContext());
        wasCalled.Should().BeTrue();
        result.Succeeded.Should().BeTrue();
    }
}
