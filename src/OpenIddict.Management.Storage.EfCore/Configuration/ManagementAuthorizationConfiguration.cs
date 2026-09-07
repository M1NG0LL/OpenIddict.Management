using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIddict.Management.Storage.EfCore.Entities;

namespace OpenIddict.Management.Storage.EfCore.Configuration;

/// <summary>
/// Entity Framework Core mapping configuration for <see cref="ManagementAuthorization{TKey}"/>.
/// </summary>
/// <typeparam name="TKey">The primary key type.</typeparam>
public class ManagementAuthorizationConfiguration<TKey> : IEntityTypeConfiguration<ManagementAuthorization<TKey>>
    where TKey : IEquatable<TKey>
{
    /// <inheritdoc/>
    public virtual void Configure(EntityTypeBuilder<ManagementAuthorization<TKey>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("OpenIddictAuthorizations");

        builder.HasIndex(a => a.CreatedAt);
    }
}
