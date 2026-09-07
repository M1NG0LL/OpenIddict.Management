using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.Management.Builder;
using OpenIddict.Management.Dashboard.Services;

namespace OpenIddict.Management.Dashboard.Extensions;

/// <summary>
/// Service collection extension methods for registering the OpenIddict Management Dashboard.
/// </summary>
public static class DashboardServiceCollectionExtensions
{
    /// <summary>
    /// Adds the OpenIddict Management Admin Dashboard services to the specified service collection.
    /// </summary>
    /// <param name="builder">The OpenIddict Management builder.</param>
    /// <param name="configure">Optional configuration action for dashboard options.</param>
    /// <returns>The <see cref="OpenIddictManagementBuilder"/> instance.</returns>
    public static OpenIddictManagementBuilder AddDashboard(
        this OpenIddictManagementBuilder builder,
        Action<DashboardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddScoped<IOpenIddictDashboardService, OpenIddictDashboardService>();

        var options = new DashboardOptions();
        configure?.Invoke(options);

        var prefix = options.PathPrefix.Trim('/');

        builder.Services.AddRazorPages(razorOptions => {
            razorOptions.Conventions.AddFolderRouteModelConvention("/", model => {
                foreach (var selector in model.Selectors)
                {
                    var template = selector.AttributeRouteModel?.Template ?? string.Empty;
                    selector.AttributeRouteModel = new AttributeRouteModel {
                        Template = string.IsNullOrEmpty(template)
                            ? prefix
                            : $"{prefix}/{template}"
                    };
                }
            });
        }).AddApplicationPart(typeof(DashboardOptions).Assembly);

        if (configure != null)
        {
            builder.Services.Configure(configure);
        }
        else
        {
            builder.Services.Configure<DashboardOptions>(_ => { });
        }

        return builder;
    }
}
