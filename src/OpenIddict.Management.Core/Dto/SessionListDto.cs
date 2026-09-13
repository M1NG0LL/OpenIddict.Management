namespace OpenIddict.Management.Dto;

/// <summary>
/// Represents a summary of an OpenIddict authorization (session) for management listings and revocations.
/// </summary>
public sealed record SessionListDto
{
    /// <summary>Gets the unique authorization identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the user / subject identifier associated with the authorization.</summary>
    public string? Subject { get; init; }

    /// <summary>Gets the client identifier associated with the authorization.</summary>
    public string? ClientId { get; init; }

    /// <summary>Gets the client display name, if available.</summary>
    public string? ClientDisplayName { get; init; }

    /// <summary>Gets the status of the authorization (e.g. valid, revoked).</summary>
    public string? Status { get; init; }

    /// <summary>Gets the scopes granted to this authorization.</summary>
    public string? Scopes { get; init; }

    private readonly DateTimeOffset _createdAt;
    private readonly DateTimeOffset? _lastModifiedAt;

    /// <summary>Gets the UTC timestamp when the authorization was created.</summary>
    public DateTimeOffset CreatedAt
    {
        get => _createdAt;
        init => _createdAt = value.ToUniversalTime();
    }

    /// <summary>Gets the UTC timestamp when the authorization was last modified, if applicable.</summary>
    public DateTimeOffset? LastModifiedAt
    {
        get => _lastModifiedAt;
        init => _lastModifiedAt = value?.ToUniversalTime();
    }

    /// <summary>Gets the count of tokens issued under this authorization.</summary>
    public int TokenCount { get; init; }

    /// <summary>Gets whether the authorization has been revoked.</summary>
    public bool IsRevoked => string.Equals(Status, "revoked", StringComparison.OrdinalIgnoreCase);
}
