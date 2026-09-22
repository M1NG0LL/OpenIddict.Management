using OpenIddict.Management.Models;

namespace OpenIddict.Management.Validation;

/// <summary>
/// Service contract for validating OpenIddict applications against configured status, environment, and custom criteria.
/// </summary>
public interface IOpenIddictApplicationValidator
{
    /// <summary>
    /// Validates an already retrieved <see cref="ManagedApplication"/> instance against active validation options.
    /// </summary>
    /// <param name="application">The application entity to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>An <see cref="ApplicationValidationResult"/> indicating whether validation passed or failed.</returns>
    ValueTask<ApplicationValidationResult> ValidateAsync(ManagedApplication application, ApplicationValidationContext context);

    /// <summary>
    /// Resolves the application by its client identifier and validates it against active validation options.
    /// </summary>
    /// <param name="clientId">The client identifier to resolve and validate.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>An <see cref="ApplicationValidationResult"/> indicating whether validation passed or failed.</returns>
    ValueTask<ApplicationValidationResult> ValidateClientIdAsync(string clientId, ApplicationValidationContext context);
}
