using OpenIddict.EntityFrameworkCore.Models;

namespace OpenIddict.Management.Storage.EfCore.Entities;

/// <summary>
/// Represents an extended OpenIddict scope entity stored in Entity Framework Core with a key type of <typeparamref name="TKey"/>.
/// </summary>
/// <typeparam name="TKey">The primary key type.</typeparam>
public class ManagementScope<TKey> : OpenIddictEntityFrameworkCoreScope<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the UTC timestamp when the scope was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the scope was last modified.
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }
}

/// <summary>
/// Represents an extended OpenIddict scope entity with a <see cref="Guid"/> key.
/// </summary>
public class ManagementScope : ManagementScope<Guid>
{
}
