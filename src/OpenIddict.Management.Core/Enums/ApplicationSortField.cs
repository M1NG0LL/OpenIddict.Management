namespace OpenIddict.Management.Enums;

/// <summary>
/// Specifies the fields by which applications can be sorted.
/// </summary>
public enum ApplicationSortField
{
    /// <summary>
    /// Sort by creation date (default).
    /// </summary>
    CreatedAt = 0,

    /// <summary>
    /// Sort by client identifier.
    /// </summary>
    ClientId = 1,

    /// <summary>
    /// Sort by display name.
    /// </summary>
    DisplayName = 2,

    /// <summary>
    /// Sort by operational status.
    /// </summary>
    Status = 3,

    /// <summary>
    /// Sort by target environment.
    /// </summary>
    Environment = 4,

    /// <summary>
    /// Sort by owner user identifier.
    /// </summary>
    OwnerUserId = 5
}
