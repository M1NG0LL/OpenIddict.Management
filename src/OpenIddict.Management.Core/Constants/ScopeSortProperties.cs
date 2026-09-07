using OpenIddict.Management.Enums;

namespace OpenIddict.Management.Constants;

/// <summary>
/// Provides string constants and list helpers for scope sorting properties.
/// </summary>
public static class ScopeSortProperties
{
    /// <summary>Sort by creation date.</summary>
    public const string CreatedAt = nameof(ScopeSortField.CreatedAt);

    /// <summary>Sort by scope name.</summary>
    public const string Name = nameof(ScopeSortField.Name);

    /// <summary>Sort by display name.</summary>
    public const string DisplayName = nameof(ScopeSortField.DisplayName);

    /// <summary>
    /// Gets all supported scope sort property names.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = [
        CreatedAt,
        Name,
        DisplayName
    ];
}
