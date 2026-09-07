using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Models;
using OpenIddict.Management.Services;
using OpenIddict.Management.Validation;
using OpenIddict.Server;

namespace OpenIddict.Management.Builder;

/// <summary>
/// Fluent builder for configuring OpenIddict Management services.
/// </summary>
/// <param name="services">The service collection.</param>
public sealed class OpenIddictManagementBuilder(IServiceCollection services)
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> being configured.
    /// </summary>
    public IServiceCollection Services { get; } = services;

    /// <summary>
    /// Registers or replaces a custom <see cref="IUserAuthenticationProvider"/> in Dependency Injection and validates that OpenIddict is configured for Authorization Code Flow with PKCE.
    /// </summary>
    /// <typeparam name="TProvider">The provider implementation type.</typeparam>
    /// <returns>The fluent builder instance.</returns>
    public OpenIddictManagementBuilder AddAuthenticationProvider<TProvider>()
        where TProvider : class, IUserAuthenticationProvider
    {
        RegisterPkceValidation();
        Services.Replace(ServiceDescriptor.Scoped<IUserAuthenticationProvider, TProvider>());
        Services.Replace(ServiceDescriptor.Scoped<IUserAuthenticationProvider<LoginContext>, TProvider>());
        return this;
    }

    /// <summary>
    /// Registers or replaces a generic <see cref="IUserAuthenticationProvider{TContext}"/> in Dependency Injection for a custom login context model and validates that OpenIddict is configured for Authorization Code Flow with PKCE.
    /// </summary>
    /// <typeparam name="TProvider">The provider implementation type.</typeparam>
    /// <typeparam name="TContext">The custom login context type.</typeparam>
    /// <returns>The fluent builder instance.</returns>
    public OpenIddictManagementBuilder AddAuthenticationProvider<TProvider, TContext>()
        where TProvider : class, IUserAuthenticationProvider<TContext>
        where TContext : class
    {
        RegisterPkceValidation();
        Services.Replace(ServiceDescriptor.Scoped<IUserAuthenticationProvider<TContext>, TProvider>());
        Services.TryAddScoped<IOpenIddictLoginEngine<TContext>, OpenIddictLoginEngine<TContext>>();
        return this;
    }

    /// <summary>
    /// Registers or replaces a custom <see cref="IOpenIddictLoginEngine"/> in Dependency Injection.
    /// </summary>
    /// <typeparam name="TEngine">The engine implementation type.</typeparam>
    /// <returns>The fluent builder instance.</returns>
    public OpenIddictManagementBuilder AddLoginEngine<TEngine>()
        where TEngine : class, IOpenIddictLoginEngine
    {
        Services.Replace(ServiceDescriptor.Scoped<IOpenIddictLoginEngine, TEngine>());
        Services.Replace(ServiceDescriptor.Scoped<IOpenIddictLoginEngine<LoginContext>, TEngine>());
        return this;
    }

    /// <summary>
    /// Registers or replaces a generic <see cref="IOpenIddictLoginEngine{TContext}"/> in Dependency Injection for a custom login context model.
    /// </summary>
    /// <typeparam name="TEngine">The engine implementation type.</typeparam>
    /// <typeparam name="TContext">The custom login context type.</typeparam>
    /// <returns>The fluent builder instance.</returns>
    public OpenIddictManagementBuilder AddLoginEngine<TEngine, TContext>()
        where TEngine : class, IOpenIddictLoginEngine<TContext>
        where TContext : class
    {
        Services.Replace(ServiceDescriptor.Scoped<IOpenIddictLoginEngine<TContext>, TEngine>());
        return this;
    }

    /// <summary>
    /// Registers or replaces a custom <see cref="IOpenIddictTokenService"/> in Dependency Injection.
    /// </summary>
    /// <typeparam name="TService">The token service implementation type.</typeparam>
    /// <returns>The fluent builder instance.</returns>
    public OpenIddictManagementBuilder AddTokenService<TService>()
        where TService : class, IOpenIddictTokenService
    {
        Services.Replace(ServiceDescriptor.Scoped<IOpenIddictTokenService, TService>());
        return this;
    }

    private void RegisterPkceValidation()
    {
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<OpenIddictServerOptions>, OpenIddictAuthorizationCodePkceValidator>());
    }
}
