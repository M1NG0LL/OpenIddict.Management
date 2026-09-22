using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Models;
using OpenIddict.Management.Options;

namespace OpenIddict.Management.Validation;

/// <summary>
/// Default implementation of <see cref="IOpenIddictApplicationValidator"/> verifying application status, environment, and custom properties.
/// </summary>
public class OpenIddictApplicationValidator : IOpenIddictApplicationValidator
{
    private readonly OpenIddictApplicationValidationOptions _options;
    private readonly IApplicationManagementService? _applicationService;
    private readonly ILogger<OpenIddictApplicationValidator>? _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="OpenIddictApplicationValidator"/>.
    /// </summary>
    /// <param name="options">The validation options.</param>
    /// <param name="applicationService">The application management service for looking up applications.</param>
    /// <param name="logger">The logger instance.</param>
    public OpenIddictApplicationValidator(
        IOptions<OpenIddictApplicationValidationOptions> options,
        IApplicationManagementService? applicationService = null,
        ILogger<OpenIddictApplicationValidator>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _applicationService = applicationService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public virtual async ValueTask<ApplicationValidationResult> ValidateClientIdAsync(string clientId, ApplicationValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_options.EnableValidation)
        {
            return ApplicationValidationResult.Success();
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return ApplicationValidationResult.Success();
        }

        if (_applicationService is null)
        {
            _logger?.LogWarning("IApplicationManagementService is not registered. Cannot resolve application '{ClientId}' for validation.", clientId);
            return _options.RejectWhenApplicationNotFound
                ? ApplicationValidationResult.Failed(_options.NotFoundErrorCode, _options.NotFoundErrorDescription)
                : ApplicationValidationResult.Success();
        }

        var lookupResult = await _applicationService.GetByClientIdAsync(clientId, context.CancellationToken);
        if (!lookupResult.IsSuccess || lookupResult.Value is null)
        {
            _logger?.LogWarning("Client application '{ClientId}' was not found during authentication validation.", clientId);
            return _options.RejectWhenApplicationNotFound
                ? ApplicationValidationResult.Failed(_options.NotFoundErrorCode, _options.NotFoundErrorDescription)
                : ApplicationValidationResult.Success();
        }

        var updatedContext = context with
        {
            ClientId = clientId,
            Application = lookupResult.Value
        };

        return await ValidateAsync(lookupResult.Value, updatedContext);
    }

    /// <inheritdoc/>
    public virtual async ValueTask<ApplicationValidationResult> ValidateAsync(ManagedApplication application, ApplicationValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_options.EnableValidation)
        {
            return ApplicationValidationResult.Success();
        }

        if (application is null)
        {
            return _options.RejectWhenApplicationNotFound
                ? ApplicationValidationResult.Failed(_options.NotFoundErrorCode, _options.NotFoundErrorDescription)
                : ApplicationValidationResult.Success();
        }

        // 1. Operational Status validation
        if (_options.ValidateStatus && !_options.AllowedStatuses.Contains(application.Status))
        {
            _logger?.LogWarning("Application '{ClientId}' rejected: Status '{Status}' is not allowed.", application.ClientId, application.Status);
            return ApplicationValidationResult.Failed(_options.StatusErrorCode, _options.StatusErrorDescription);
        }

        // 2. Deployment Environment validation
        if (_options.ValidateEnvironment)
        {
            if (_options.ExpectedEnvironment.HasValue && application.Environment != _options.ExpectedEnvironment.Value)
            {
                _logger?.LogWarning(
                    "Application '{ClientId}' rejected: Environment '{Environment}' does not match expected environment '{Expected}'.",
                    application.ClientId,
                    application.Environment,
                    _options.ExpectedEnvironment.Value);
                return ApplicationValidationResult.Failed(_options.EnvironmentErrorCode, _options.EnvironmentErrorDescription);
            }

            if (!_options.AllowedEnvironments.Contains(application.Environment))
            {
                _logger?.LogWarning(
                    "Application '{ClientId}' rejected: Environment '{Environment}' is not in allowed environments.",
                    application.ClientId,
                    application.Environment);
                return ApplicationValidationResult.Failed(_options.EnvironmentErrorCode, _options.EnvironmentErrorDescription);
            }
        }

        // 3. Custom developer validation hook
        if (_options.CustomValidator is not null)
        {
            var customResult = await _options.CustomValidator(application, context);
            if (!customResult.Succeeded)
            {
                _logger?.LogWarning(
                    "Application '{ClientId}' rejected by custom validator: {Error} - {Description}",
                    application.ClientId,
                    customResult.Error,
                    customResult.ErrorDescription);
                return customResult;
            }
        }

        return ApplicationValidationResult.Success();
    }
}
