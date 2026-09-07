using System.Text.Json;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Management.Enums;

namespace OpenIddict.Management.Storage.EfCore.Entities;

/// <summary>
/// Represents an extended OpenIddict application entity stored in Entity Framework Core with a key type of <typeparamref name="TKey"/>.
/// </summary>
/// <typeparam name="TKey">The primary key type.</typeparam>
public class ManagementApplication<TKey> : OpenIddictEntityFrameworkCoreApplication<TKey, ManagementAuthorization<TKey>, ManagementToken<TKey>>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the operational status of the application.
    /// </summary>
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Active;

    /// <summary>
    /// Gets or sets the target environment of the application.
    /// </summary>
    public ApplicationEnvironment Environment { get; set; } = ApplicationEnvironment.Development;

    /// <summary>
    /// Gets or sets the JSON representation of allowed roles for the application.
    /// </summary>
    public string? AllowedRolesJson { get; set; }

    /// <summary>
    /// Gets or sets a description of the application.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the logo URI for the application.
    /// </summary>
    public string? LogoUri { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the application owner.
    /// </summary>
    public string? OwnerUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the application was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the application was last modified.
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for the application.
    /// </summary>
    public string? ExtraData { get; set; }

    /// <summary>
    /// Gets or sets the list of tags associated with the application.
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Checks whether the application contains the specified tag.
    /// </summary>
    /// <param name="tag">The tag name to check.</param>
    /// <returns>True if the application contains the tag; otherwise, false.</returns>
    public bool HasTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || Tags is null or { Count: 0 })
        {
            return false;
        }

        return Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the list of allowed roles parsed from <see cref="AllowedRolesJson"/>.
    /// </summary>
    /// <returns>A list of role names.</returns>
    public List<string> GetAllowedRoles()
    {
        if (string.IsNullOrWhiteSpace(AllowedRolesJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(AllowedRolesJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Sets the list of allowed roles by serializing it into <see cref="AllowedRolesJson"/>.
    /// </summary>
    /// <param name="roles">The role names to set.</param>
    public void SetAllowedRoles(IEnumerable<string>? roles)
    {
        if (roles is null)
        {
            AllowedRolesJson = null;
            return;
        }

        AllowedRolesJson = JsonSerializer.Serialize(roles);
    }
}

/// <summary>
/// Represents an extended OpenIddict application entity with a <see cref="Guid"/> key.
/// </summary>
public class ManagementApplication : ManagementApplication<Guid>
{
}
