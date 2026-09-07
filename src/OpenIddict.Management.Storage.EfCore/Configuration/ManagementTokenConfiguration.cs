using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIddict.Management.Storage.EfCore.Entities;

namespace OpenIddict.Management.Storage.EfCore.Configuration;

/// <summary>
/// Entity Framework Core mapping configuration for <see cref="ManagementToken{TKey}"/>.
/// </summary>
/// <typeparam name="TKey">The primary key type.</typeparam>
public class ManagementTokenConfiguration<TKey> : IEntityTypeConfiguration<ManagementToken<TKey>>
    where TKey : IEquatable<TKey>
{
    /// <inheritdoc/>
    public virtual void Configure(EntityTypeBuilder<ManagementToken<TKey>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("OpenIddictTokens");

        builder.HasIndex(t => t.CreatedAt);
        builder.HasIndex(t => t.RevokedAt);
    }
}
