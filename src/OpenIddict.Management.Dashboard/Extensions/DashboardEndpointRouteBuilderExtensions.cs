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
    /// Registers static file handling and dashboard authorization middleware in the ASP.NET Core request pipeline.
    /// Call this when configuring middleware explicitly on <see cref="IApplicationBuilder"/>.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
    public static IApplicationBuilder UseOpenIddictManagementDashboard(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseStaticFiles();
        app.UseMiddleware<Middleware.DashboardMiddleware>();

        return app;
    }

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

        // Automatically register static files and dashboard protection middleware if endpoints implements IApplicationBuilder (e.g., WebApplication)
        if (endpoints is IApplicationBuilder app)
        {
            app.UseOpenIddictManagementDashboard();
        }

        endpoints.MapRazorPages();

        return endpoints;
    }
}
