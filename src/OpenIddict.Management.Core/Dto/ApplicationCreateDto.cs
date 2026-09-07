using OpenIddict.Management.Enums;

namespace OpenIddict.Management.Dto;

/// <summary>
/// Data transfer object for creating a new application.
/// </summary>
public sealed record ApplicationCreateDto
{
    /// <summary>
    /// Gets the unique client identifier for the application.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Gets the client secret for the application, if applicable.
    /// </summary>
    public string? ClientSecret { get; init; }

    /// <summary>
    /// Gets the display name of the application.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the operational environment. Defaults to Development.
    /// </summary>
    public ApplicationEnvironment Environment { get; init; } = ApplicationEnvironment.Development;

    /// <summary>
    /// Gets the allowed roles for the application.
    /// </summary>
    public IReadOnlyList<string> AllowedRoles { get; init; } = [];

    /// <summary>
    /// Gets a description of the application.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the logo URI for the application.
    /// </summary>
    public string? LogoUri { get; init; }

    /// <summary>
    /// Gets the owner user ID.
    /// </summary>
    public string? OwnerUserId { get; init; }

    /// <summary>
    /// Gets the allowed redirect URIs.
    /// </summary>
    public IReadOnlyList<string> RedirectUris { get; init; } = [];

    /// <summary>
    /// Gets the allowed post-logout redirect URIs.
    /// </summary>
    public IReadOnlyList<string> PostLogoutRedirectUris { get; init; } = [];

    /// <summary>
    /// Gets the permissions granted to the application.
    /// </summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];

    /// <summary>
    /// Gets optional extra data for the application.
    /// </summary>
    public string? ExtraData { get; init; }

    /// <summary>
    /// Gets the list of tags assigned to the application.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Gets the requirements enforced on the application.
    /// </summary>
    public IReadOnlyList<string> Requirements { get; init; } = [];
}
