using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIddict.Management.Storage.EfCore.Entities;

namespace OpenIddict.Management.Storage.EfCore.Configuration;

/// <summary>
/// Entity Framework Core mapping configuration for <see cref="ManagementScope{TKey}"/>.
/// </summary>
/// <typeparam name="TKey">The primary key type.</typeparam>
public class ManagementScopeConfiguration<TKey> : IEntityTypeConfiguration<ManagementScope<TKey>>
    where TKey : IEquatable<TKey>
{
    /// <inheritdoc/>
    public virtual void Configure(EntityTypeBuilder<ManagementScope<TKey>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("OpenIddictScopes");
    }
}
