using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIddict.Management.Storage.EfCore.Entities;

namespace OpenIddict.Management.Storage.EfCore.Configuration;

/// <summary>
/// Entity Framework Core mapping configuration for <see cref="ManagementApplication{TKey}"/>.
/// </summary>
/// <typeparam name="TKey">The primary key type.</typeparam>
public class ManagementApplicationConfiguration<TKey> : IEntityTypeConfiguration<ManagementApplication<TKey>>
    where TKey : IEquatable<TKey>
{
    /// <inheritdoc/>
    public virtual void Configure(EntityTypeBuilder<ManagementApplication<TKey>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("OpenIddictApplications");

        builder.Property(a => a.Description).HasMaxLength(500);
        builder.Property(a => a.LogoUri).HasMaxLength(2000);
        builder.Property(a => a.OwnerUserId).HasMaxLength(450);
        builder.Property(a => a.DefaultScopes);

        builder.PrimitiveCollection(a => a.Tags);

        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.Environment);
        builder.HasIndex(a => a.OwnerUserId);
    }
}
