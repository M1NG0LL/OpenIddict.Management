using OpenIddict.Management.Models;

namespace OpenIddict.Management.Contracts;

/// <summary>
/// Generic service contract for coordinating user login and credential verification with a custom context type.
/// </summary>
/// <typeparam name="TContext">The custom login context type.</typeparam>
public interface IOpenIddictLoginEngine<in TContext> where TContext : class
{
    /// <summary>
    /// Authenticates a user given the provided login context.
    /// </summary>
    /// <param name="context">The login credentials and request context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="LoginResult"/> representing the outcome of the authentication attempt.</returns>
    Task<LoginResult> AuthenticateAsync(TContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default non-generic service contract for coordinating user login and credential verification with standard <see cref="LoginContext"/>.
/// </summary>
public interface IOpenIddictLoginEngine : IOpenIddictLoginEngine<LoginContext>
{
}
