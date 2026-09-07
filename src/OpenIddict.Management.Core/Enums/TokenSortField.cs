namespace OpenIddict.Management.Enums;

/// <summary>
/// Specifies the fields by which tokens can be sorted.
/// </summary>
public enum TokenSortField
{
    /// <summary>
    /// Sort by creation date (default).
    /// </summary>
    CreationDate = 0,

    /// <summary>
    /// Sort by subject (user ID).
    /// </summary>
    Subject = 1,

    /// <summary>
    /// Sort by token type.
    /// </summary>
    Type = 2,

    /// <summary>
    /// Sort by status.
    /// </summary>
    Status = 3,

    /// <summary>
    /// Sort by expiration date.
    /// </summary>
    ExpirationDate = 4
}
