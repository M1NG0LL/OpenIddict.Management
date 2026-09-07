using OpenIddict.Management.Enums;

namespace OpenIddict.Management.Dto;

/// <summary>
/// Data transfer object for updating an existing application.
/// </summary>
public sealed record ApplicationUpdateDto
{
    /// <summary>
    /// Gets the updated display name of the application.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the updated status of the application.
    /// </summary>
    public ApplicationStatus Status { get; init; } = ApplicationStatus.Active;

    /// <summary>
    /// Gets the updated environment of the application.
    /// </summary>
    public ApplicationEnvironment Environment { get; init; } = ApplicationEnvironment.Development;

    /// <summary>
    /// Gets the updated allowed roles for the application.
    /// </summary>
    public IReadOnlyList<string> AllowedRoles { get; init; } = [];

    /// <summary>
    /// Gets the updated description of the application.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the updated logo URI for the application.
    /// </summary>
    public string? LogoUri { get; init; }

    /// <summary>
    /// Gets the updated owner user ID.
    /// </summary>
    public string? OwnerUserId { get; init; }

    /// <summary>
    /// Gets the updated allowed redirect URIs.
    /// </summary>
    public IReadOnlyList<string> RedirectUris { get; init; } = [];

    /// <summary>
    /// Gets the updated allowed post-logout redirect URIs.
    /// </summary>
    public IReadOnlyList<string> PostLogoutRedirectUris { get; init; } = [];

    /// <summary>
    /// Gets the updated permissions granted to the application.
    /// </summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];

    /// <summary>
    /// Gets updated optional extra data for the application.
    /// </summary>
    public string? ExtraData { get; init; }

    /// <summary>
    /// Gets the updated list of tags assigned to the application.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Gets the updated requirements enforced on the application.
    /// </summary>
    public IReadOnlyList<string> Requirements { get; init; } = [];
}
