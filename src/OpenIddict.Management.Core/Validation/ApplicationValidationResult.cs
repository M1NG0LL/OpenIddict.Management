namespace OpenIddict.Management.Validation;

/// <summary>
/// Represents the result of an OpenIddict application validation check.
/// </summary>
public sealed record ApplicationValidationResult
{
    private static readonly ApplicationValidationResult SuccessfulResult = new() { Succeeded = true };

    /// <summary>
    /// Gets a value indicating whether the application validation was successful.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the error code if validation failed (e.g. invalid_client, unauthorized_client).
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets a detailed description explaining the reason for validation failure.
    /// </summary>
    public string? ErrorDescription { get; init; }

    /// <summary>
    /// Gets an optional URI to a page containing additional details about the error.
    /// </summary>
    public string? ErrorUri { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A successful <see cref="ApplicationValidationResult"/>.</returns>
    public static ApplicationValidationResult Success() => SuccessfulResult;

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="error">The error code.</param>
    /// <param name="errorDescription">The error description.</param>
    /// <param name="errorUri">Optional URI to human-readable error info.</param>
    /// <returns>A failed <see cref="ApplicationValidationResult"/>.</returns>
    public static ApplicationValidationResult Failed(string error, string errorDescription, string? errorUri = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorDescription);

        return new ApplicationValidationResult
        {
            Succeeded = false,
            Error = error,
            ErrorDescription = errorDescription,
            ErrorUri = errorUri
        };
    }
}
