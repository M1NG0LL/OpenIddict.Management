namespace OpenIddict.Management.Dashboard.Services;

/// <summary>
/// Overview metrics for the OpenIddict Management Dashboard.
/// </summary>
public sealed record DashboardOverviewDto
{
    /// <summary>Gets the count of active client applications.</summary>
    public int ActiveApplications { get; init; }

    /// <summary>Gets the total count of registered client applications.</summary>
    public int TotalApplications { get; init; }

    /// <summary>Gets the total count of configured scopes.</summary>
    public int ConfiguredScopes { get; init; }

    /// <summary>Gets the total count of tracked tokens.</summary>
    public int Tokens { get; init; }

    /// <summary>Gets the count of revoked tokens.</summary>
    public int RevokedTokens { get; init; }

    /// <summary>Gets the count of active authorizations.</summary>
    public int ActiveAuthorizations { get; init; }
}
