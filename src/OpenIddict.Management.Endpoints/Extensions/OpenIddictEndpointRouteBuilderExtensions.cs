using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Management.Endpoints.Filters;
using OpenIddict.Management.Endpoints.Groups;

namespace OpenIddict.Management.Endpoints.Extensions;

/// <summary>
/// Extension methods for mapping OpenIddict Management Minimal API endpoints on <see cref="IEndpointRouteBuilder"/>.
/// </summary>
public static class OpenIddictEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps all OpenIddict Management HTTP Minimal API endpoints (`/applications`, `/tokens`, `/revocation`, `/scopes`)
    /// under the configured route prefix.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="configure">Optional configuration action for endpoint options.</param>
    /// <returns>A <see cref="RouteGroupBuilder"/> representing the mapped root management group.</returns>
    public static RouteGroupBuilder MapOpenIddictManagementEndpoints(
        this IEndpointRouteBuilder endpoints,
        Action<ManagementEndpointOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = new ManagementEndpointOptions();
        var registeredOptions = endpoints.ServiceProvider.GetService<IOptions<ManagementEndpointOptions>>()?.Value;
        if (registeredOptions is not null)
        {
            options.RoutePrefix = registeredOptions.RoutePrefix;
            options.AuthorizationPolicy = registeredOptions.AuthorizationPolicy;
            options.RequireAuthorization = registeredOptions.RequireAuthorization;
            options.Tags = registeredOptions.Tags;
        }

        configure?.Invoke(options);

        var prefix = options.RoutePrefix.StartsWith('/') ? options.RoutePrefix : $"/{options.RoutePrefix}";
        var group = endpoints.MapGroup(prefix);

        group.AddEndpointFilter<ExceptionMappingEndpointFilter>();
        group.AddEndpointFilter<ValidationEndpointFilter>();

        if (options.Tags is { Length: > 0 })
        {
            group.WithTags(options.Tags);
        }

        if (options.RequireAuthorization)
        {
            if (!string.IsNullOrWhiteSpace(options.AuthorizationPolicy))
            {
                group.RequireAuthorization(options.AuthorizationPolicy);
            }
            else
            {
                group.RequireAuthorization();
            }
        }

        // Map endpoint sub-groups
        group.MapApplicationEndpoints();
        group.MapTokenEndpoints();
        group.MapRevocationEndpoints();
        group.MapScopeEndpoints();

        return group;
    }
}
