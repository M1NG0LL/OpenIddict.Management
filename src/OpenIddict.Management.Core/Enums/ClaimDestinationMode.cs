namespace OpenIddict.Management.Enums;

/// <summary>
/// Specifies the token destination(s) for claims produced during authentication and token generation.
/// </summary>
[Flags]
public enum ClaimDestinationMode
{
    /// <summary>
    /// Embeds the claim only in the access token (Default).
    /// </summary>
    AccessToken = 1,

    /// <summary>
    /// Embeds the claim only in the OpenID identity token.
    /// </summary>
    IdentityToken = 2,

    /// <summary>
    /// Embeds the claim in both the access token and identity token.
    /// </summary>
    AccessTokenAndIdentityToken = AccessToken | IdentityToken
}
