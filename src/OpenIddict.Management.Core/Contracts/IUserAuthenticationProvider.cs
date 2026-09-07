using OpenIddict.Management.Models;

namespace OpenIddict.Management.Contracts;

/// <summary>
/// Generic service contract implemented by host applications to perform custom user authentication using a custom context model.
/// </summary>
/// <typeparam name="TContext">The custom login context type.</typeparam>
public interface IUserAuthenticationProvider<in TContext> where TContext : class
{
    /// <summary>
    /// Authenticates a user given the provided login context.
    /// </summary>
    /// <param name="context">The login credentials and request context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="LoginResult"/> representing the outcome of the authentication attempt with token metadata.</returns>
    Task<LoginResult> AuthenticateAsync(TContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default non-generic service contract implemented by host applications to perform custom user authentication with <see cref="LoginContext"/>.
/// </summary>
public interface IUserAuthenticationProvider : IUserAuthenticationProvider<LoginContext>
{
}
