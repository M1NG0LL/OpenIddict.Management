using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OpenIddict.Management.Dashboard.Extensions;

/// <summary>
/// Extension methods for mapping OpenIddict Management Dashboard Razor Pages, static files, and security middleware in a single call.
/// </summary>
public static class DashboardEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the OpenIddict Management Dashboard Razor Pages, registers static file handling, and attaches dashboard authorization middleware under the configured route prefix.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="configure">Optional configuration action for dashboard options.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> instance.</returns>
    public static IEndpointRouteBuilder MapOpenIddictManagementDashboard(
        this IEndpointRouteBuilder endpoints,
        Action<DashboardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = new DashboardOptions();
        var registeredOptions = endpoints.ServiceProvider.GetService<IOptions<DashboardOptions>>()?.Value;
        if (registeredOptions != null)
        {
            options.PathPrefix = registeredOptions.PathPrefix;
            options.DashboardTitle = registeredOptions.DashboardTitle;
            options.AuthorizationPolicy = registeredOptions.AuthorizationPolicy;
            options.RequireAuthorization = registeredOptions.RequireAuthorization;
            options.EnabledFeatures = registeredOptions.EnabledFeatures;
        }

        configure?.Invoke(options);

        // Automatically register static files and dashboard protection middleware
        if (endpoints is IApplicationBuilder app)
        {
            app.UseStaticFiles();
            app.UseMiddleware<Middleware.DashboardMiddleware>();
        }

        endpoints.MapRazorPages();

        return endpoints;
    }
}
