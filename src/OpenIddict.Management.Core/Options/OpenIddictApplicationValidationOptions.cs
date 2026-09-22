using OpenIddict.Abstractions;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Models;
using OpenIddict.Management.Validation;

namespace OpenIddict.Management.Options;

/// <summary>
/// Configuration options for OpenIddict application status, environment, and custom property validation.
/// </summary>
public sealed class OpenIddictApplicationValidationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether application validation is enabled. Defaults to <c>true</c>.
    /// </summary>
    public bool EnableValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to validate the application's operational status. Defaults to <c>true</c>.
    /// </summary>
    public bool ValidateStatus { get; set; } = true;

    /// <summary>
    /// Gets or sets the set of allowed application statuses. Defaults to allowing only <see cref="ApplicationStatus.Active"/>.
    /// </summary>
    public HashSet<ApplicationStatus> AllowedStatuses { get; set; } = [ApplicationStatus.Active];

    /// <summary>
    /// Gets or sets a value indicating whether to validate the application's deployment environment. Defaults to <c>false</c>.
    /// </summary>
    public bool ValidateEnvironment { get; set; } = false;

    /// <summary>
    /// Gets or sets the set of allowed environments. Defaults to all environments (<see cref="ApplicationEnvironment.Development"/>, <see cref="ApplicationEnvironment.Staging"/>, and <see cref="ApplicationEnvironment.Production"/>).
    /// </summary>
    public HashSet<ApplicationEnvironment> AllowedEnvironments { get; set; } =
    [
        ApplicationEnvironment.Development,
        ApplicationEnvironment.Staging,
        ApplicationEnvironment.Production
    ];

    /// <summary>
    /// Gets or sets an optional expected environment that the application must strictly match. If set, this overrides <see cref="AllowedEnvironments"/>.
    /// </summary>
    public ApplicationEnvironment? ExpectedEnvironment { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether requests should be rejected if the client application cannot be found in the store. Defaults to <c>true</c>.
    /// </summary>
    public bool RejectWhenApplicationNotFound { get; set; } = true;

    /// <summary>
    /// Gets or sets the OAuth2 error code returned when application status validation fails. Defaults to <see cref="OpenIddictConstants.Errors.UnauthorizedClient"/>.
    /// </summary>
    public string StatusErrorCode { get; set; } = OpenIddictConstants.Errors.UnauthorizedClient;

    /// <summary>
    /// Gets or sets the error description returned when application status validation fails.
    /// </summary>
    public string StatusErrorDescription { get; set; } = "The client application is disabled or deleted and cannot authenticate.";

    /// <summary>
    /// Gets or sets the OAuth2 error code returned when application environment validation fails. Defaults to <see cref="OpenIddictConstants.Errors.UnauthorizedClient"/>.
    /// </summary>
    public string EnvironmentErrorCode { get; set; } = OpenIddictConstants.Errors.UnauthorizedClient;

    /// <summary>
    /// Gets or sets the error description returned when application environment validation fails.
    /// </summary>
    public string EnvironmentErrorDescription { get; set; } = "The client application is not authorized to operate in this environment.";

    /// <summary>
    /// Gets or sets the OAuth2 error code returned when an application is not found. Defaults to <see cref="OpenIddictConstants.Errors.InvalidClient"/>.
    /// </summary>
    public string NotFoundErrorCode { get; set; } = OpenIddictConstants.Errors.InvalidClient;

    /// <summary>
    /// Gets or sets the error description returned when an application is not found.
    /// </summary>
    public string NotFoundErrorDescription { get; set; } = "The specified client application was not found.";

    /// <summary>
    /// Gets or sets an optional custom validation delegate for validating additional application properties.
    /// </summary>
    public Func<ManagedApplication, ApplicationValidationContext, ValueTask<ApplicationValidationResult>>? CustomValidator { get; set; }

    /// <summary>
    /// Configures the allowed application statuses for authentication.
    /// </summary>
    /// <param name="statuses">The permitted application statuses.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    public OpenIddictApplicationValidationOptions RequireStatus(params ApplicationStatus[] statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);
        ValidateStatus = true;
        AllowedStatuses = new HashSet<ApplicationStatus>(statuses);
        return this;
    }

    /// <summary>
    /// Configures an explicit required deployment environment for the application.
    /// </summary>
    /// <param name="environment">The required environment.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    public OpenIddictApplicationValidationOptions RequireEnvironment(ApplicationEnvironment environment)
    {
        ValidateEnvironment = true;
        ExpectedEnvironment = environment;
        AllowedEnvironments = [environment];
        return this;
    }

    /// <summary>
    /// Configures the allowed deployment environments for the application.
    /// </summary>
    /// <param name="environments">The permitted environments.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    public OpenIddictApplicationValidationOptions RequireEnvironments(params ApplicationEnvironment[] environments)
    {
        ArgumentNullException.ThrowIfNull(environments);
        ValidateEnvironment = true;
        ExpectedEnvironment = null;
        AllowedEnvironments = new HashSet<ApplicationEnvironment>(environments);
        return this;
    }

    /// <summary>
    /// Registers a custom asynchronous validation delegate to inspect application properties.
    /// </summary>
    /// <param name="validator">The custom validation function.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    public OpenIddictApplicationValidationOptions ValidateCustom(
        Func<ManagedApplication, ApplicationValidationContext, ValueTask<ApplicationValidationResult>> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        CustomValidator = validator;
        return this;
    }

    /// <summary>
    /// Registers a custom synchronous validation delegate to inspect application properties.
    /// </summary>
    /// <param name="validator">The custom validation function.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    public OpenIddictApplicationValidationOptions ValidateCustom(
        Func<ManagedApplication, ApplicationValidationContext, ApplicationValidationResult> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        CustomValidator = (app, ctx) => ValueTask.FromResult(validator(app, ctx));
        return this;
    }
}
