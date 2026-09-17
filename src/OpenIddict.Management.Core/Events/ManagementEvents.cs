using OpenIddict.Management.Models;

namespace OpenIddict.Management.Events;

/// <summary>
/// Event raised when an application is created.
/// </summary>
public sealed record ApplicationCreatedEvent(
    ManagedApplication Application,
    string? Actor = null) : IManagementEvent
{
    /// <inheritdoc/>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    /// <inheritdoc/>
    public string Category => "Application";
    /// <inheritdoc/>
    public string Action => "Created";
}

/// <summary>
/// Event raised when an application is updated.
/// </summary>
public sealed record ApplicationUpdatedEvent(
    ManagedApplication Application,
    string? Actor = null) : IManagementEvent
{
    /// <inheritdoc/>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    /// <inheritdoc/>
    public string Category => "Application";
    /// <inheritdoc/>
    public string Action => "Updated";
}

/// <summary>
/// Event raised when an application is deleted.
/// </summary>
public sealed record ApplicationDeletedEvent(
    string ApplicationId,
    string? ClientId = null,
    string? Actor = null) : IManagementEvent
{
    /// <inheritdoc/>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    /// <inheritdoc/>
    public string Category => "Application";
    /// <inheritdoc/>
    public string Action => "Deleted";
}

/// <summary>
/// Event raised when an application's client secret is rotated.
/// </summary>
public sealed record ApplicationSecretRotatedEvent(
    string ApplicationId,
    string? ClientId = null,
    string? Actor = null) : IManagementEvent
{
    /// <inheritdoc/>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    /// <inheritdoc/>
    public string Category => "Application";
    /// <inheritdoc/>
    public string Action => "SecretRotated";
}

/// <summary>
/// Event raised when an OpenID Connect scope is created.
/// </summary>
public sealed record ScopeCreatedEvent(
    ManagedScope Scope,
    string? Actor = null) : IManagementEvent
{
    /// <inheritdoc/>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    /// <inheritdoc/>
    public string Category => "Scope";
    /// <inheritdoc/>
    public string Action => "Created";
}

/// <summary>
/// Event raised when an OpenID Connect scope is updated.
/// </summary>
public sealed record ScopeUpdatedEvent(
    ManagedScope Scope,
    string? Actor = null) : IManagementEvent
{
    /// <inheritdoc/>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    /// <inheritdoc/>
    public string Category => "Scope";
    /// <inheritdoc/>
    public string Action => "Updated";
}

/// <summary>
/// Event raised when an OpenID Connect scope is deleted.
/// </summary>
public sealed record ScopeDeletedEvent(
    string ScopeId,
    string? Name = null,
    string? Actor = null) : IManagementEvent
{
    /// <inheritdoc/>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    /// <inheritdoc/>
    public string Category => "Scope";
    /// <inheritdoc/>
    public string Action => "Deleted";
}

/// <summary>
/// Event raised when a single token is revoked.
/// </summary>
public sealed record TokenRevokedEvent(
    string TokenId,
    string? Subject = null,
    string? ClientId = null,
    string? Actor = null) : IManagementEvent
{
    /// <inheritdoc/>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    /// <inheritdoc/>
    public string Category => "Token";
    /// <inheritdoc/>
    public string Action => "Revoked";
}

/// <summary>
/// Event raised when multiple tokens are revoked in batch or by filter.
/// </summary>
public sealed record TokensRevokedEvent(
    int Count,
    string? Scope = null,
    string? Actor = null) : IManagementEvent
{
    /// <inheritdoc/>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    /// <inheritdoc/>
    public string Category => "Token";
    /// <inheritdoc/>
    public string Action => "RevokedBatch";
}

/// <summary>
/// Event raised when tokens are pruned from storage.
/// </summary>
public sealed record TokensPrunedEvent(
    int PrunedCount,
    string? Actor = null) : IManagementEvent
{
    /// <inheritdoc/>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    /// <inheritdoc/>
    public string Category => "Token";
    /// <inheritdoc/>
    public string Action => "Pruned";
}

/// <summary>
/// Event raised when a session authorization is revoked.
/// </summary>
public sealed record SessionRevokedEvent(
    string? SessionId,
    string? UserId = null,
    int TokensRevoked = 0,
    string? Actor = null) : IManagementEvent
{
    /// <inheritdoc/>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    /// <inheritdoc/>
    public string Category => "Authorization";
    /// <inheritdoc/>
    public string Action => "Revoked";
}

/// <summary>
/// Event raised when a session authorization is permanently deleted.
/// </summary>
public sealed record SessionDeletedEvent(
    string SessionId,
    string? Actor = null) : IManagementEvent
{
    /// <inheritdoc/>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    /// <inheritdoc/>
    public string Category => "Authorization";
    /// <inheritdoc/>
    public string Action => "Deleted";
}
