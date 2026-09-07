using System.Security.Claims;
using OpenIddict.Management.Models;

namespace OpenIddict.Management.Contracts;

/// <summary>
/// Service contract for constructing OpenIddict-compliant <see cref="ClaimsPrincipal"/> instances tailored for Authorization Code Flow with PKCE.
/// </summary>
public interface IOpenIddictTokenService
{
    /// <summary>
    /// Constructs a fully configured <see cref="ClaimsPrincipal"/> using the specified <see cref="TokenCreationParameters"/>.
    /// </summary>
    /// <param name="parameters">The parameters including subject, username, roles, scopes, and extra metadata.</param>
    /// <returns>A <see cref="ClaimsPrincipal"/> configured with appropriate OpenIddict claim destinations.</returns>
    ClaimsPrincipal CreatePrincipal(TokenCreationParameters parameters);

    /// <summary>
    /// Constructs a fully configured <see cref="ClaimsPrincipal"/> from a successful <see cref="LoginResult"/>.
    /// </summary>
    /// <param name="loginResult">The successful login result containing user and token metadata.</param>
    /// <returns>A <see cref="ClaimsPrincipal"/> configured with appropriate OpenIddict claim destinations.</returns>
    ClaimsPrincipal CreatePrincipal(LoginResult loginResult);
}
