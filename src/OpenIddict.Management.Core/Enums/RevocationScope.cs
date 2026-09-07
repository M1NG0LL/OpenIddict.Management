namespace OpenIddict.Management.Enums;

/// <summary>
/// Specifies the scope of a revocation operation.
/// </summary>
public enum RevocationScope
{
    /// <summary>
    /// Revocation applies to a single token.
    /// </summary>
    Token = 0,

    /// <summary>
    /// Revocation applies to all tokens for a specific user.
    /// </summary>
    User = 1,

    /// <summary>
    /// Revocation applies to all tokens for a specific client application.
    /// </summary>
    Client = 2,

    /// <summary>
    /// Revocation applies to session authorizations.
    /// </summary>
    Session = 3
}
