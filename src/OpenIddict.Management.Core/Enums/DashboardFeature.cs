namespace OpenIddict.Management.Enums;

/// <summary>
/// Specifies feature modules available in the OpenIddict Management Admin Dashboard.
/// </summary>
[Flags]
public enum DashboardFeature
{
    /// <summary>
    /// No features enabled.
    /// </summary>
    None = 0,

    /// <summary>
    /// Application management feature module.
    /// </summary>
    ApplicationManagement = 1 << 0,

    /// <summary>
    /// Token inspector feature module.
    /// </summary>
    TokenInspector = 1 << 1,

    /// <summary>
    /// Session manager feature module.
    /// </summary>
    SessionManager = 1 << 2,

    /// <summary>
    /// Scope manager feature module.
    /// </summary>
    ScopeManager = 1 << 3,

    /// <summary>
    /// Audit trail feature module.
    /// </summary>
    AuditTrail = 1 << 4,

    /// <summary>
    /// All dashboard features enabled.
    /// </summary>
    All = ApplicationManagement | TokenInspector | SessionManager | ScopeManager | AuditTrail
}
