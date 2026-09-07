using OpenIddict.EntityFrameworkCore.Models;

namespace OpenIddict.Management.Storage.EfCore.Entities;

/// <summary>
/// Represents an extended OpenIddict token entity stored in Entity Framework Core with a key type of <typeparamref name="TKey"/>.
/// </summary>
/// <typeparam name="TKey">The primary key type.</typeparam>
public class ManagementToken<TKey> : OpenIddictEntityFrameworkCoreToken<TKey, ManagementApplication<TKey>, ManagementAuthorization<TKey>>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the UTC timestamp when the token was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the token was revoked, if applicable.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }
}

/// <summary>
/// Represents an extended OpenIddict token entity with a <see cref="Guid"/> key.
/// </summary>
public class ManagementToken : ManagementToken<Guid>
{
}
