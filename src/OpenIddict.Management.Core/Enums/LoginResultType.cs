namespace OpenIddict.Management.Enums;

/// <summary>
/// Specifies the result type of a login authentication attempt.
/// </summary>
public enum LoginResultType
{
    /// <summary>
    /// Authentication succeeded.
    /// </summary>
    Success = 0,

    /// <summary>
    /// Authentication failed due to invalid credentials.
    /// </summary>
    InvalidCredentials = 1,

    /// <summary>
    /// Authentication failed because the account is locked out.
    /// </summary>
    LockedOut = 2,

    /// <summary>
    /// Two-factor authentication is required.
    /// </summary>
    RequiresTwoFactor = 3,

    /// <summary>
    /// Authentication failed due to custom logic or configuration.
    /// </summary>
    Failed = 4
}
