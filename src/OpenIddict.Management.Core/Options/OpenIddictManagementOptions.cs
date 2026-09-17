namespace OpenIddict.Management.Options;

/// <summary>
/// Global configuration options for OpenIddict.Management suite.
/// </summary>
public sealed class OpenIddictManagementOptions
{
    /// <summary>
    /// Gets or sets the HTTP API route prefix. Defaults to "/api/management".
    /// </summary>
    public string RoutePrefix { get; set; } = "/api/management";

    /// <summary>
    /// Gets or sets a value indicating whether endpoints require HTTPS transport security. Defaults to true.
    /// </summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether audit logging and event dispatching are enabled. Defaults to true.
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;
}
