namespace OpenIddict.Management.Endpoints;

/// <summary>
/// Options for configuring OpenIddict Management HTTP Minimal API endpoints.
/// </summary>
public sealed class ManagementEndpointOptions
{
    /// <summary>
    /// Gets or sets the route prefix for management endpoints. Defaults to "/api/management".
    /// </summary>
    public string RoutePrefix { get; set; } = "/api/management";

    /// <summary>
    /// Gets or sets the ASP.NET Core authorization policy name required to access endpoints.
    /// If null, default authorization or anonymous access will apply based on <see cref="RequireAuthorization"/>.
    /// </summary>
    public string? AuthorizationPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether endpoints require authenticated authorization. Defaults to true.
    /// </summary>
    public bool RequireAuthorization { get; set; } = true;

    /// <summary>
    /// Gets or sets OpenAPI tags applied to all endpoints. Defaults to ["OpenIddict Management"].
    /// </summary>
    public string[] Tags { get; set; } = ["OpenIddict Management"];
}
