namespace OpenIddict.Management.Enums;

/// <summary>
/// Specifies the fields by which scopes can be sorted.
/// </summary>
public enum ScopeSortField
{
    /// <summary>
    /// Sort by creation date (default).
    /// </summary>
    CreatedAt = 0,

    /// <summary>
    /// Sort by scope name.
    /// </summary>
    Name = 1,

    /// <summary>
    /// Sort by display name.
    /// </summary>
    DisplayName = 2
}
