using OpenIddict.Management.Contracts;
using OpenIddict.Management.Models;

namespace OpenIddict.Management.Services;

/// <summary>
/// Generic implementation of <see cref="IOpenIddictLoginEngine{TContext}"/> that coordinates user authentication with registered <see cref="IUserAuthenticationProvider{TContext}"/> instances.
/// </summary>
/// <typeparam name="TContext">The custom login context type.</typeparam>
public class OpenIddictLoginEngine<TContext>(IUserAuthenticationProvider<TContext>? authenticationProvider = null) : IOpenIddictLoginEngine<TContext>
    where TContext : class
{
    /// <inheritdoc/>
    public virtual async Task<LoginResult> AuthenticateAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is LoginContext standardContext)
        {
            if (string.IsNullOrWhiteSpace(standardContext.Username) || string.IsNullOrWhiteSpace(standardContext.Password))
            {
                return LoginResult.InvalidCredentials("Username and password are required.");
            }
        }

        if (authenticationProvider is not null)
        {
            return await authenticationProvider.AuthenticateAsync(context, cancellationToken);
        }

        return LoginResult.Failed("NoAuthenticationProvider", $"No IUserAuthenticationProvider<{typeof(TContext).Name}> implementation has been registered by the host application.");
    }
}

/// <summary>
/// Default non-generic implementation of <see cref="IOpenIddictLoginEngine"/> that coordinates user authentication with standard <see cref="LoginContext"/>.
/// </summary>
public class OpenIddictLoginEngine(IUserAuthenticationProvider? authenticationProvider = null) 
    : OpenIddictLoginEngine<LoginContext>(authenticationProvider), IOpenIddictLoginEngine
{
}
