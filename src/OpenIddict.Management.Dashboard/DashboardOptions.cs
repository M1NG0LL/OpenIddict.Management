using OpenIddict.Management.Enums;

namespace OpenIddict.Management.Dashboard;

/// <summary>
/// Options for configuring the OpenIddict Management admin dashboard UI.
/// </summary>
public sealed class DashboardOptions
{
    /// <summary>
    /// Gets or sets the path prefix for mounting the dashboard middleware and pages. Defaults to "/management".
    /// </summary>
    public string PathPrefix { get; set; } = "/management";

    /// <summary>
    /// Gets or sets the display title shown on the admin dashboard UI. Defaults to "OpenIddict Management".
    /// </summary>
    public string DashboardTitle { get; set; } = "OpenIddict Management";

    /// <summary>
    /// Gets or sets the URL to navigate to when exiting the dashboard. If null or empty, the exit button is hidden. Defaults to "/".
    /// </summary>
    public string? ExitUrl { get; set; } = "/";

    /// <summary>
    /// Gets or sets the display text for the exit dashboard button. Defaults to "Exit Dashboard".
    /// </summary>
    public string ExitButtonText { get; set; } = "Exit Dashboard";

    /// <summary>
    /// Gets or sets the ASP.NET Core authorization policy name required to access the dashboard.
    /// </summary>
    public string? AuthorizationPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether authorization is required to access the dashboard. Defaults to true.
    /// </summary>
    public bool RequireAuthorization { get; set; } = true;

    /// <summary>
    /// Gets or sets the dashboard feature modules that are enabled. Defaults to <see cref="DashboardFeature.All"/>.
    /// </summary>
    public DashboardFeature EnabledFeatures { get; set; } = DashboardFeature.All;

    /// <summary>
    /// Gets or sets the list of predefined application tags available for selection in the dashboard. Defaults to empty.
    /// </summary>
    public List<string> AvailableTags { get; set; } = [];
}
