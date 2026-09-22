using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.Management.Builder;
using OpenIddict.Management.Options;
using OpenIddict.Management.Validation;
using OpenIddict.Server;
using OpenIddict.Validation;

namespace OpenIddict.Management.Extensions;

/// <summary>
/// Extension methods for configuring OpenIddict application status, environment, and custom property validation.
/// </summary>
public static class OpenIddictApplicationValidationExtensions
{
    /// <summary>
    /// Adds and configures custom OpenIddict application validation for both Server (login across any flow) and Validation pipelines.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for validation options.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddOpenIddictApplicationValidation(
        this IServiceCollection services,
        Action<OpenIddictApplicationValidationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddScoped<IOpenIddictApplicationValidator, OpenIddictApplicationValidator>();
        services.TryAddScoped<OpenIddictApplicationValidationHandler>();

        // Register event handlers for OpenIddict Server (Authorization Code, Client Credentials, Refresh Token, Password, Device Code flows)
        var serverBuilder = new OpenIddictServerBuilder(services);
        serverBuilder.AddEventHandler<OpenIddictServerEvents.ValidateTokenRequestContext>(options =>
            options.UseScopedHandler<OpenIddictApplicationValidationHandler>()
                   .SetOrder(OpenIddictApplicationValidationHandler.DefaultOrder));

        serverBuilder.AddEventHandler<OpenIddictServerEvents.ValidateAuthorizationRequestContext>(options =>
            options.UseScopedHandler<OpenIddictApplicationValidationHandler>()
                   .SetOrder(OpenIddictApplicationValidationHandler.DefaultOrder));

        serverBuilder.AddEventHandler<OpenIddictServerEvents.ValidateDeviceRequestContext>(options =>
            options.UseScopedHandler<OpenIddictApplicationValidationHandler>()
                   .SetOrder(OpenIddictApplicationValidationHandler.DefaultOrder));

        // Register event handlers for OpenIddict Validation (access token validation)
        var validationBuilder = new OpenIddictValidationBuilder(services);
        validationBuilder.AddEventHandler<OpenIddictValidationEvents.ProcessAuthenticationContext>(options =>
            options.UseScopedHandler<OpenIddictApplicationValidationHandler>()
                   .SetOrder(OpenIddictApplicationValidationHandler.DefaultOrder));

        return services;
    }

    /// <summary>
    /// Adds and configures custom OpenIddict application validation within the OpenIddict Server pipeline.
    /// </summary>
    /// <param name="builder">The OpenIddict server builder.</param>
    /// <param name="configure">Optional configuration action for validation options.</param>
    /// <returns>The server builder instance for chaining.</returns>
    public static OpenIddictServerBuilder AddApplicationValidation(
        this OpenIddictServerBuilder builder,
        Action<OpenIddictApplicationValidationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        builder.Services.TryAddScoped<IOpenIddictApplicationValidator, OpenIddictApplicationValidator>();
        builder.Services.TryAddScoped<OpenIddictApplicationValidationHandler>();

        builder.AddEventHandler<OpenIddictServerEvents.ValidateTokenRequestContext>(options =>
            options.UseScopedHandler<OpenIddictApplicationValidationHandler>()
                   .SetOrder(OpenIddictApplicationValidationHandler.DefaultOrder));

        builder.AddEventHandler<OpenIddictServerEvents.ValidateAuthorizationRequestContext>(options =>
            options.UseScopedHandler<OpenIddictApplicationValidationHandler>()
                   .SetOrder(OpenIddictApplicationValidationHandler.DefaultOrder));

        builder.AddEventHandler<OpenIddictServerEvents.ValidateDeviceRequestContext>(options =>
            options.UseScopedHandler<OpenIddictApplicationValidationHandler>()
                   .SetOrder(OpenIddictApplicationValidationHandler.DefaultOrder));

        return builder;
    }

    /// <summary>
    /// Adds and configures custom OpenIddict application validation within the OpenIddict Validation pipeline.
    /// </summary>
    /// <param name="builder">The OpenIddict validation builder.</param>
    /// <param name="configure">Optional configuration action for validation options.</param>
    /// <returns>The validation builder instance for chaining.</returns>
    public static OpenIddictValidationBuilder AddApplicationValidation(
        this OpenIddictValidationBuilder builder,
        Action<OpenIddictApplicationValidationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        builder.Services.TryAddScoped<IOpenIddictApplicationValidator, OpenIddictApplicationValidator>();
        builder.Services.TryAddScoped<OpenIddictApplicationValidationHandler>();

        builder.AddEventHandler<OpenIddictValidationEvents.ProcessAuthenticationContext>(options =>
            options.UseScopedHandler<OpenIddictApplicationValidationHandler>()
                   .SetOrder(OpenIddictApplicationValidationHandler.DefaultOrder));

        return builder;
    }

    /// <summary>
    /// Adds and configures custom OpenIddict application validation for both Server and Validation pipelines via <see cref="OpenIddictManagementBuilder"/>.
    /// </summary>
    /// <param name="builder">The OpenIddict management builder.</param>
    /// <param name="configure">Optional configuration action for validation options.</param>
    /// <returns>The management builder instance for chaining.</returns>
    public static OpenIddictManagementBuilder AddApplicationValidation(
        this OpenIddictManagementBuilder builder,
        Action<OpenIddictApplicationValidationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddOpenIddictApplicationValidation(configure);
        return builder;
    }
}
