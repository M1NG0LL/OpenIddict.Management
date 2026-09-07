namespace OpenIddict.Management.Enums;

/// <summary>
/// Specifies the operational status of a managed application.
/// </summary>
public enum ApplicationStatus
{
    /// <summary>
    /// The application is active and allowed to request tokens.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The application is disabled and cannot request new tokens.
    /// </summary>
    Disabled = 1,

    /// <summary>
    /// The application has been soft-deleted.
    /// </summary>
    Deleted = 2
}
