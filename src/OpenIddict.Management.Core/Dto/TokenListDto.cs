namespace OpenIddict.Management.Dto;

/// <summary>
/// Represents a summary of an issued token for management listings and inspections.
/// </summary>
public sealed record TokenListDto
{
    /// <summary>Gets the unique token identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the token reference identifier, if applicable.</summary>
    public string? ReferenceId { get; init; }

    /// <summary>Gets the user/subject identifier associated with the token.</summary>
    public string? Subject { get; init; }

    /// <summary>Gets the client identifier associated with the token.</summary>
    public string? ClientId { get; init; }

    /// <summary>Gets the client display name, if available.</summary>
    public string? ClientDisplayName { get; init; }

    /// <summary>Gets the token type (e.g. access_token, refresh_token).</summary>
    public string? Type { get; init; }

    /// <summary>Gets the operational status of the token.</summary>
    public string? Status { get; init; }

    private readonly DateTimeOffset? _createdAt;
    private readonly DateTimeOffset? _expirationDate;
    private readonly DateTimeOffset? _revokedAt;

    /// <summary>Gets the UTC timestamp when the token was created.</summary>
    public DateTimeOffset? CreatedAt
    {
        get => _createdAt;
        init => _createdAt = value?.ToUniversalTime();
    }

    /// <summary>Gets the UTC timestamp when the token expires.</summary>
    public DateTimeOffset? ExpirationDate
    {
        get => _expirationDate;
        init => _expirationDate = value?.ToUniversalTime();
    }

    /// <summary>Gets the UTC timestamp when the token was revoked, if applicable.</summary>
    public DateTimeOffset? RevokedAt
    {
        get => _revokedAt;
        init => _revokedAt = value?.ToUniversalTime();
    }

    /// <summary>Gets the serialized token payload, if available.</summary>
    public string? Payload { get; init; }

    /// <summary>Gets the serialized properties associated with the token, if available.</summary>
    public string? Properties { get; init; }

    /// <summary>Gets the serialized data of the token (payload or properties).</summary>
    public string? SerializedData => !string.IsNullOrWhiteSpace(Payload) ? Payload : (!string.IsNullOrWhiteSpace(Properties) ? Properties : null);

    /// <summary>Gets whether the token has been revoked.</summary>
    public bool IsRevoked => string.Equals(Status, "revoked", StringComparison.OrdinalIgnoreCase) || RevokedAt.HasValue;

    /// <summary>Gets whether the token has expired.</summary>
    public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value.UtcDateTime < DateTime.UtcNow;
}
