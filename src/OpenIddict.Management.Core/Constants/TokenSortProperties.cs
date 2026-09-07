using OpenIddict.Management.Enums;

namespace OpenIddict.Management.Constants;

/// <summary>
/// Provides string constants and list helpers for token sorting properties.
/// </summary>
public static class TokenSortProperties
{
    /// <summary>Sort by creation date.</summary>
    public const string CreationDate = nameof(TokenSortField.CreationDate);

    /// <summary>Sort by subject (user ID).</summary>
    public const string Subject = nameof(TokenSortField.Subject);

    /// <summary>Sort by token type.</summary>
    public const string Type = nameof(TokenSortField.Type);

    /// <summary>Sort by status.</summary>
    public const string Status = nameof(TokenSortField.Status);

    /// <summary>Sort by expiration date.</summary>
    public const string ExpirationDate = nameof(TokenSortField.ExpirationDate);

    /// <summary>
    /// Gets all supported token sort property names.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = [
        CreationDate,
        Subject,
        Type,
        Status,
        ExpirationDate
    ];
}
