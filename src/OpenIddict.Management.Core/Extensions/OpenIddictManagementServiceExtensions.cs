using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.Management.Builder;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Events;
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

        services.TryAddSingleton<IAuditTrailStore, InMemoryAuditTrailStore>();
        services.TryAddScoped<IManagementEventPublisher, OpenIddict.Management.Events.ManagementEventPublisher>();
        services.TryAddScoped<IConfigurationExportImportService, ConfigurationExportImportService>();

        // Register default audit trail handler for domain events
        services.TryAddScoped<OpenIddict.Management.Events.AuditTrailEventHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<OpenIddict.Management.Events.IManagementEventHandler<OpenIddict.Management.Events.ApplicationCreatedEvent>, OpenIddict.Management.Events.AuditTrailEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<OpenIddict.Management.Events.IManagementEventHandler<OpenIddict.Management.Events.ApplicationUpdatedEvent>, OpenIddict.Management.Events.AuditTrailEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<OpenIddict.Management.Events.IManagementEventHandler<OpenIddict.Management.Events.ApplicationDeletedEvent>, OpenIddict.Management.Events.AuditTrailEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<OpenIddict.Management.Events.IManagementEventHandler<OpenIddict.Management.Events.ApplicationSecretRotatedEvent>, OpenIddict.Management.Events.AuditTrailEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<OpenIddict.Management.Events.IManagementEventHandler<OpenIddict.Management.Events.ScopeCreatedEvent>, OpenIddict.Management.Events.AuditTrailEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<OpenIddict.Management.Events.IManagementEventHandler<OpenIddict.Management.Events.ScopeUpdatedEvent>, OpenIddict.Management.Events.AuditTrailEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<OpenIddict.Management.Events.IManagementEventHandler<OpenIddict.Management.Events.ScopeDeletedEvent>, OpenIddict.Management.Events.AuditTrailEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<OpenIddict.Management.Events.IManagementEventHandler<OpenIddict.Management.Events.TokenRevokedEvent>, OpenIddict.Management.Events.AuditTrailEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<OpenIddict.Management.Events.IManagementEventHandler<OpenIddict.Management.Events.TokensRevokedEvent>, OpenIddict.Management.Events.AuditTrailEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<OpenIddict.Management.Events.IManagementEventHandler<OpenIddict.Management.Events.TokensPrunedEvent>, OpenIddict.Management.Events.AuditTrailEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<OpenIddict.Management.Events.IManagementEventHandler<OpenIddict.Management.Events.SessionRevokedEvent>, OpenIddict.Management.Events.AuditTrailEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<OpenIddict.Management.Events.IManagementEventHandler<OpenIddict.Management.Events.SessionDeletedEvent>, OpenIddict.Management.Events.AuditTrailEventHandler>());

        services.TryAddSingleton<ITokenCleanupJobManager, TokenCleanupJobManager>();
        AddTokenCleanupHostedService(services);

        var optionsBuilder = services.AddOptions<OpenIddictManagementOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        return new OpenIddictManagementBuilder(services);
    }

    /// <summary>
    /// Configures and registers the automatic token cleanup background service and manager.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration delegate for token cleanup options.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddTokenCleanup(
        this IServiceCollection services,
        Action<TokenCleanupOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<ITokenCleanupJobManager, TokenCleanupJobManager>();
        AddTokenCleanupHostedService(services);

        return services;
    }

    private static void AddTokenCleanupHostedService(IServiceCollection services)
    {
        if (!services.Any(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) && d.ImplementationType == typeof(TokenCleanupBackgroundService)))
        {
            services.AddHostedService<TokenCleanupBackgroundService>();
        }
    }
}
