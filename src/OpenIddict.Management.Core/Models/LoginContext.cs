namespace OpenIddict.Management.Models;

/// <summary>
/// Represents the context and credentials supplied during a user login attempt.
/// </summary>
public sealed record LoginContext
{
    /// <summary>
    /// Gets the username, email address, or user identifier.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets the user's password.
    /// </summary>
    public required string Password { get; init; }

    /// <summary>
    /// Gets a value indicating whether the login session should persist across browser restarts.
    /// </summary>
    public bool RememberMe { get; init; }

    /// <summary>
    /// Gets the optional two-factor authentication (2FA) code.
    /// </summary>
    public string? TwoFactorCode { get; init; }

    /// <summary>
    /// Gets the optional two-factor recovery code.
    /// </summary>
    public string? TwoFactorRecoveryCode { get; init; }

    /// <summary>
    /// Gets the optional client application identifier requesting authentication.
    /// </summary>
    public string? ClientApplicationId { get; init; }

    /// <summary>
    /// Gets the optional IP address of the user initiating login.
    /// </summary>
    public string? IPAddress { get; init; }

    /// <summary>
    /// Gets the optional User-Agent header of the user initiating login.
    /// </summary>
    public string? UserAgent { get; init; }
}
