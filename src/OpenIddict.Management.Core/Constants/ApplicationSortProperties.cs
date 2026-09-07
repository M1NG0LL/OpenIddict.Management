using OpenIddict.Management.Enums;

namespace OpenIddict.Management.Constants;

/// <summary>
/// Provides string constants and list helpers for application sorting properties.
/// </summary>
public static class ApplicationSortProperties
{
    /// <summary>Sort by creation date.</summary>
    public const string CreatedAt = nameof(ApplicationSortField.CreatedAt);

    /// <summary>Sort by client identifier.</summary>
    public const string ClientId = nameof(ApplicationSortField.ClientId);

    /// <summary>Sort by display name.</summary>
    public const string DisplayName = nameof(ApplicationSortField.DisplayName);

    /// <summary>Sort by operational status.</summary>
    public const string Status = nameof(ApplicationSortField.Status);

    /// <summary>Sort by target environment.</summary>
    public const string Environment = nameof(ApplicationSortField.Environment);

    /// <summary>Sort by owner user identifier.</summary>
    public const string OwnerUserId = nameof(ApplicationSortField.OwnerUserId);

    /// <summary>
    /// Gets all supported application sort property names.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = [
        CreatedAt,
        ClientId,
        DisplayName,
        Status,
        Environment,
        OwnerUserId
    ];
}
