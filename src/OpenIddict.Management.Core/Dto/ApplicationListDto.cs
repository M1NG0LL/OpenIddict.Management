using OpenIddict.Management.Enums;

namespace OpenIddict.Management.Dto;

/// <summary>
/// Data transfer object for lightweight application listing.
/// </summary>
public sealed record ApplicationListDto
{
    /// <summary>
    /// Gets the unique identifier of the application.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the client identifier of the application.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Gets the display name of the application.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the status of the application.
    /// </summary>
    public ApplicationStatus Status { get; init; }

    /// <summary>
    /// Gets the environment of the application.
    /// </summary>
    public ApplicationEnvironment Environment { get; init; }

    /// <summary>
    /// Gets the owner user ID.
    /// </summary>
    public string? OwnerUserId { get; init; }

    /// <summary>
    /// Gets the list of tags assigned to the application.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
