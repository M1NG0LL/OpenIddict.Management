using System.Security.Claims;
using OpenIddict.Management.Models;

namespace OpenIddict.Management.Contracts;

/// <summary>
/// Service contract for constructing OpenIddict-compliant <see cref="ClaimsPrincipal"/> instances tailored for Authorization Code Flow with PKCE.
/// </summary>
public interface IOpenIddictTokenService
{
    /// <summary>
    /// Constructs a fully configured <see cref="ClaimsPrincipal"/> using the specified <see cref="TokenCreationParameters"/> asynchronously,
    /// resolving default scopes for the specified <see cref="TokenCreationParameters.ClientId"/> if applicable.
    /// </summary>
    /// <param name="parameters">The token creation parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes with the configured <see cref="ClaimsPrincipal"/>.</returns>
    Task<ClaimsPrincipal> CreatePrincipalAsync(TokenCreationParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Constructs a fully configured <see cref="ClaimsPrincipal"/> from a successful <see cref="LoginResult"/> asynchronously,
    /// resolving default scopes for the specified client ID if applicable.
    /// </summary>
    /// <param name="loginResult">The login result.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes with the configured <see cref="ClaimsPrincipal"/>.</returns>
    Task<ClaimsPrincipal> CreatePrincipalAsync(LoginResult loginResult, CancellationToken cancellationToken = default);
}
