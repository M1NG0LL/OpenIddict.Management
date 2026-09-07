using OpenIddict.EntityFrameworkCore.Models;

namespace OpenIddict.Management.Storage.EfCore.Entities;

/// <summary>
/// Represents an extended OpenIddict authorization entity stored in Entity Framework Core with a key type of <typeparamref name="TKey"/>.
/// </summary>
/// <typeparam name="TKey">The primary key type.</typeparam>
public class ManagementAuthorization<TKey> : OpenIddictEntityFrameworkCoreAuthorization<TKey, ManagementApplication<TKey>, ManagementToken<TKey>>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the UTC timestamp when the authorization was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the authorization was last modified.
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }
}

/// <summary>
/// Represents an extended OpenIddict authorization entity with a <see cref="Guid"/> key.
/// </summary>
public class ManagementAuthorization : ManagementAuthorization<Guid>
{
}
