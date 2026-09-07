namespace OpenIddict.Management.Enums;

/// <summary>
/// Specifies the deployment environment of a managed application.
/// </summary>
public enum ApplicationEnvironment
{
    /// <summary>
    /// Development environment.
    /// </summary>
    Development = 0,

    /// <summary>
    /// Staging or testing environment.
    /// </summary>
    Staging = 1,

    /// <summary>
    /// Production environment.
    /// </summary>
    Production = 2
}
