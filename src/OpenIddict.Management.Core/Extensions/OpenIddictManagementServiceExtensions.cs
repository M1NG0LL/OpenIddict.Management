using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.Management.Builder;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Models;
using OpenIddict.Management.Options;
using OpenIddict.Management.Services;

namespace OpenIddict.Management.Extensions;

/// <summary>
/// Extension methods for setting up OpenIddict Management services in Dependency Injection.
/// </summary>
public static class OpenIddictManagementServiceExtensions
{
    /// <summary>
    /// Registers core OpenIddict Management services and returns an <see cref="OpenIddictManagementBuilder"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The <see cref="OpenIddictManagementBuilder"/> for chaining additional setups.</returns>
    public static OpenIddictManagementBuilder AddOpenIddictManagement(
        this IServiceCollection services,
        Action<OpenIddictManagementOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IOpenIddictLoginEngine, OpenIddictLoginEngine>();
        services.TryAddScoped<IOpenIddictLoginEngine<LoginContext>, OpenIddictLoginEngine>();
        services.TryAddScoped<IOpenIddictTokenService, OpenIddictTokenService>();

        var optionsBuilder = services.AddOptions<OpenIddictManagementOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        return new OpenIddictManagementBuilder(services);
    }
}
