using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OpenIddict.Management.Dashboard.Middleware;

/// <summary>
/// Middleware for protecting and intercepting HTTP requests targeting the OpenIddict Management Dashboard.
/// </summary>
/// <param name="next">The next middleware in the pipeline.</param>
public sealed class DashboardMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Invokes the dashboard middleware for authorization and routing checks.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="options">Dashboard configuration options.</param>
    public async Task InvokeAsync(HttpContext context, IOptions<DashboardOptions> options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        var config = options.Value;
        var prefix = config.PathPrefix.StartsWith('/') ? config.PathPrefix : $"/{config.PathPrefix}";

        if (context.Request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
        {
            if (config.RequireAuthorization)
            {
                var authService = context.RequestServices?.GetService<IAuthorizationService>();
                if (authService != null)
                {
                    if (!string.IsNullOrWhiteSpace(config.AuthorizationPolicy))
                    {
                        var policyProvider = context.RequestServices?.GetService<IAuthorizationPolicyProvider>();
                        if (policyProvider != null)
                        {
                            var policy = await policyProvider.GetPolicyAsync(config.AuthorizationPolicy);
                            if (policy != null)
                            {
                                var authResult = await authService.AuthorizeAsync(context.User, null, policy);
                                if (!authResult.Succeeded)
                                {
                                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                    return;
                                }
                            }
                        }
                    }
                    else if (context.User.Identity is not { IsAuthenticated: true })
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(config.AuthorizationPolicy) || context.User.Identity is not { IsAuthenticated: true })
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }
        }

        await next(context);
    }
}
