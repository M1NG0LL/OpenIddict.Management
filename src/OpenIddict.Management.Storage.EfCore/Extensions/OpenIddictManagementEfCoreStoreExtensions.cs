using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.Management.Builder;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Extensions;
using OpenIddict.Management.Options;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Stores;

namespace OpenIddict.Management.Storage.EfCore.Extensions;

/// <summary>
/// Extension methods for registering OpenIddict Management EF Core stores in Dependency Injection.
/// </summary>
public static class OpenIddictManagementEfCoreStoreExtensions
{
    /// <summary>
    /// Registers default EF Core implementations of OpenIddict Management services (<see cref="IApplicationManagementService"/>,
    /// <see cref="IOpenIddictRevocationManager"/>, <see cref="IScopeManagementService"/>) and configures OpenIddict Core
    /// default entity types using <see cref="Guid"/> key type.
    /// </summary>
    /// <typeparam name="TContext">The target DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddOpenIddictManagementStores<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        return services.AddOpenIddictManagementStores<TContext, Guid>();
    }

    /// <summary>
    /// Registers default EF Core implementations of OpenIddict Management services using the specified <typeparamref name="TKey"/> key type
    /// and configures OpenIddict Core default entity types.
    /// </summary>
    /// <typeparam name="TContext">The target DbContext type.</typeparam>
    /// <typeparam name="TKey">The primary key type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddOpenIddictManagementStores<TContext, TKey>(this IServiceCollection services)
        where TContext : DbContext
        where TKey : IEquatable<TKey>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);

        services.TryAddScoped<IApplicationManagementService, EfCoreApplicationManagementStore<TContext, TKey>>();
        services.TryAddScoped<IOpenIddictRevocationManager, EfCoreRevocationStore<TContext, TKey>>();
        services.TryAddScoped<IScopeManagementService, EfCoreScopeManagementStore<TContext, TKey>>();

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<TContext>()
                       .ReplaceDefaultEntities<TKey>();

                options.SetDefaultApplicationEntity<ManagementApplication<TKey>>()
                       .SetDefaultAuthorizationEntity<ManagementAuthorization<TKey>>()
                       .SetDefaultScopeEntity<ManagementScope<TKey>>()
                       .SetDefaultTokenEntity<ManagementToken<TKey>>();
            });

        return services;
    }

    /// <summary>
    /// Registers core OpenIddict Management services, default EF Core stores for <typeparamref name="TContext"/>,
    /// and configures OpenIddict Core default entity types using <see cref="Guid"/> key type.
    /// </summary>
    /// <typeparam name="TContext">The target DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for management options.</param>
    /// <returns>The <see cref="OpenIddictManagementBuilder"/> instance.</returns>
    public static OpenIddictManagementBuilder AddOpenIddictManagement<TContext>(
        this IServiceCollection services,
        Action<OpenIddictManagementOptions>? configure = null)
        where TContext : DbContext
    {
        return services.AddOpenIddictManagement<TContext, Guid>(configure);
    }

    /// <summary>
    /// Registers core OpenIddict Management services, default EF Core stores for <typeparamref name="TContext"/>,
    /// and configures OpenIddict Core default entity types using the specified <typeparamref name="TKey"/> key type.
    /// </summary>
    /// <typeparam name="TContext">The target DbContext type.</typeparam>
    /// <typeparam name="TKey">The primary key type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for management options.</param>
    /// <returns>The <see cref="OpenIddictManagementBuilder"/> instance.</returns>
    public static OpenIddictManagementBuilder AddOpenIddictManagement<TContext, TKey>(
        this IServiceCollection services,
        Action<OpenIddictManagementOptions>? configure = null)
        where TContext : DbContext
        where TKey : IEquatable<TKey>
    {
        var builder = services.AddOpenIddictManagement(configure);
        services.AddOpenIddictManagementStores<TContext, TKey>();
        return builder;
    }

    /// <summary>
    /// Adds EF Core management stores and registers default management entities for <typeparamref name="TContext"/>
    /// using <see cref="Guid"/> key type to the OpenIddict Management builder.
    /// </summary>
    /// <typeparam name="TContext">The target DbContext type.</typeparam>
    /// <param name="builder">The management builder.</param>
    /// <returns>The <see cref="OpenIddictManagementBuilder"/> instance.</returns>
    public static OpenIddictManagementBuilder AddEfCoreStores<TContext>(
        this OpenIddictManagementBuilder builder)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddOpenIddictManagementStores<TContext, Guid>();
        return builder;
    }

    /// <summary>
    /// Adds EF Core management stores and registers default management entities for <typeparamref name="TContext"/>
    /// using the specified <typeparamref name="TKey"/> key type to the OpenIddict Management builder.
    /// </summary>
    /// <typeparam name="TContext">The target DbContext type.</typeparam>
    /// <typeparam name="TKey">The primary key type.</typeparam>
    /// <param name="builder">The management builder.</param>
    /// <returns>The <see cref="OpenIddictManagementBuilder"/> instance.</returns>
    public static OpenIddictManagementBuilder AddEfCoreStores<TContext, TKey>(
        this OpenIddictManagementBuilder builder)
        where TContext : DbContext
        where TKey : IEquatable<TKey>
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddOpenIddictManagementStores<TContext, TKey>();
        return builder;
    }

    /// <summary>
    /// Registers custom store implementations for all OpenIddict Management services, replacing any existing store registrations.
    /// </summary>
    /// <typeparam name="TAppStore">The application management service implementation type.</typeparam>
    /// <typeparam name="TRevocationStore">The revocation manager implementation type.</typeparam>
    /// <typeparam name="TScopeStore">The scope management service implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddOpenIddictManagementStores<TAppStore, TRevocationStore, TScopeStore>(this IServiceCollection services)
        where TAppStore : class, IApplicationManagementService
        where TRevocationStore : class, IOpenIddictRevocationManager
        where TScopeStore : class, IScopeManagementService
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);

        services.Replace(ServiceDescriptor.Scoped<IApplicationManagementService, TAppStore>());
        services.Replace(ServiceDescriptor.Scoped<IOpenIddictRevocationManager, TRevocationStore>());
        services.Replace(ServiceDescriptor.Scoped<IScopeManagementService, TScopeStore>());

        return services;
    }

    /// <summary>
    /// Registers or replaces a custom implementation for <see cref="IApplicationManagementService"/>.
    /// </summary>
    /// <typeparam name="TStore">The application management service implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddApplicationManagementStore<TStore>(this IServiceCollection services)
        where TStore : class, IApplicationManagementService
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.Replace(ServiceDescriptor.Scoped<IApplicationManagementService, TStore>());
        return services;
    }

    /// <summary>
    /// Registers or replaces a custom implementation for <see cref="IOpenIddictRevocationManager"/>.
    /// </summary>
    /// <typeparam name="TStore">The revocation manager implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddRevocationStore<TStore>(this IServiceCollection services)
        where TStore : class, IOpenIddictRevocationManager
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.Replace(ServiceDescriptor.Scoped<IOpenIddictRevocationManager, TStore>());
        return services;
    }

    /// <summary>
    /// Registers or replaces a custom implementation for <see cref="IScopeManagementService"/>.
    /// </summary>
    /// <typeparam name="TStore">The scope management service implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddScopeManagementStore<TStore>(this IServiceCollection services)
        where TStore : class, IScopeManagementService
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.Replace(ServiceDescriptor.Scoped<IScopeManagementService, TStore>());
        return services;
    }
}
