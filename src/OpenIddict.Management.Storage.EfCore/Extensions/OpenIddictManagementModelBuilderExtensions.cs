using Microsoft.EntityFrameworkCore;
using OpenIddict.Management.Storage.EfCore.Configuration;
using OpenIddict.Management.Storage.EfCore.Entities;

namespace OpenIddict.Management.Storage.EfCore.Extensions;

/// <summary>
/// Extension methods for configuring OpenIddict Management models on <see cref="ModelBuilder"/>.
/// </summary>
public static class OpenIddictManagementModelBuilderExtensions
{
    /// <summary>
    /// Registers OpenIddict Management entities and model configurations on the specified <see cref="ModelBuilder"/>
    /// using <see cref="Guid"/> as the primary key type.
    /// </summary>
    /// <param name="builder">The <see cref="ModelBuilder"/> instance.</param>
    /// <returns>The modified <see cref="ModelBuilder"/>.</returns>
    public static ModelBuilder UseOpenIddictManagement(this ModelBuilder builder)
    {
        return builder.UseOpenIddictManagement<Guid>();
    }

    /// <summary>
    /// Registers OpenIddict Management entities and model configurations on the specified <see cref="ModelBuilder"/>
    /// using the specified <typeparamref name="TKey"/> as the primary key type.
    /// </summary>
    /// <typeparam name="TKey">The primary key type.</typeparam>
    /// <param name="builder">The <see cref="ModelBuilder"/> instance.</param>
    /// <returns>The modified <see cref="ModelBuilder"/>.</returns>
    public static ModelBuilder UseOpenIddictManagement<TKey>(this ModelBuilder builder)
        where TKey : IEquatable<TKey>
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseOpenIddict<ManagementApplication<TKey>, ManagementAuthorization<TKey>, ManagementScope<TKey>, ManagementToken<TKey>, TKey>();
        builder.ApplyConfiguration(new ManagementApplicationConfiguration<TKey>());
        builder.ApplyConfiguration(new ManagementAuthorizationConfiguration<TKey>());
        builder.ApplyConfiguration(new ManagementScopeConfiguration<TKey>());
        builder.ApplyConfiguration(new ManagementTokenConfiguration<TKey>());

        return builder;
    }
}
