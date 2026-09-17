using OpenIddict.Management.Contracts;
using OpenIddict.Management.Models;

namespace OpenIddict.Management.Events;

/// <summary>
/// Built-in event handler that records all OpenIddict management domain events to the configured <see cref="IAuditTrailStore"/>.
/// </summary>
public sealed class AuditTrailEventHandler(IAuditTrailStore auditTrailStore) :
    IManagementEventHandler<ApplicationCreatedEvent>,
    IManagementEventHandler<ApplicationUpdatedEvent>,
    IManagementEventHandler<ApplicationDeletedEvent>,
    IManagementEventHandler<ApplicationSecretRotatedEvent>,
    IManagementEventHandler<ScopeCreatedEvent>,
    IManagementEventHandler<ScopeUpdatedEvent>,
    IManagementEventHandler<ScopeDeletedEvent>,
    IManagementEventHandler<TokenRevokedEvent>,
    IManagementEventHandler<TokensRevokedEvent>,
    IManagementEventHandler<TokensPrunedEvent>,
    IManagementEventHandler<SessionRevokedEvent>,
    IManagementEventHandler<SessionDeletedEvent>
{
    /// <inheritdoc/>
    public Task HandleAsync(ApplicationCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        return auditTrailStore.RecordAsync(new ManagementAuditEntry
        {
            Category = @event.Category,
            Action = @event.Action,
            EntityId = @event.Application.Id,
            EntityName = @event.Application.ClientId,
            Actor = @event.Actor,
            Timestamp = @event.Timestamp,
            Details = $"Application '{@event.Application.ClientId}' created."
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(ApplicationUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        return auditTrailStore.RecordAsync(new ManagementAuditEntry
        {
            Category = @event.Category,
            Action = @event.Action,
            EntityId = @event.Application.Id,
            EntityName = @event.Application.ClientId,
            Actor = @event.Actor,
            Timestamp = @event.Timestamp,
            Details = $"Application '{@event.Application.ClientId}' updated."
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(ApplicationDeletedEvent @event, CancellationToken cancellationToken = default)
    {
        return auditTrailStore.RecordAsync(new ManagementAuditEntry
        {
            Category = @event.Category,
            Action = @event.Action,
            EntityId = @event.ApplicationId,
            EntityName = @event.ClientId,
            Actor = @event.Actor,
            Timestamp = @event.Timestamp,
            Details = $"Application '{@event.ClientId ?? @event.ApplicationId}' deleted."
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(ApplicationSecretRotatedEvent @event, CancellationToken cancellationToken = default)
    {
        return auditTrailStore.RecordAsync(new ManagementAuditEntry
        {
            Category = @event.Category,
            Action = @event.Action,
            EntityId = @event.ApplicationId,
            EntityName = @event.ClientId,
            Actor = @event.Actor,
            Timestamp = @event.Timestamp,
            Details = $"Client secret rotated for application '{@event.ClientId ?? @event.ApplicationId}'."
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(ScopeCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        return auditTrailStore.RecordAsync(new ManagementAuditEntry
        {
            Category = @event.Category,
            Action = @event.Action,
            EntityId = @event.Scope.Id,
            EntityName = @event.Scope.Name,
            Actor = @event.Actor,
            Timestamp = @event.Timestamp,
            Details = $"Scope '{@event.Scope.Name}' created."
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(ScopeUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        return auditTrailStore.RecordAsync(new ManagementAuditEntry
        {
            Category = @event.Category,
            Action = @event.Action,
            EntityId = @event.Scope.Id,
            EntityName = @event.Scope.Name,
            Actor = @event.Actor,
            Timestamp = @event.Timestamp,
            Details = $"Scope '{@event.Scope.Name}' updated."
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(ScopeDeletedEvent @event, CancellationToken cancellationToken = default)
    {
        return auditTrailStore.RecordAsync(new ManagementAuditEntry
        {
            Category = @event.Category,
            Action = @event.Action,
            EntityId = @event.ScopeId,
            EntityName = @event.Name,
            Actor = @event.Actor,
            Timestamp = @event.Timestamp,
            Details = $"Scope '{@event.Name ?? @event.ScopeId}' deleted."
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(TokenRevokedEvent @event, CancellationToken cancellationToken = default)
    {
        return auditTrailStore.RecordAsync(new ManagementAuditEntry
        {
            Category = @event.Category,
            Action = @event.Action,
            EntityId = @event.TokenId,
            Actor = @event.Actor,
            Timestamp = @event.Timestamp,
            Details = $"Token '{@event.TokenId}' revoked. Subject: {@event.Subject}, Client: {@event.ClientId}."
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(TokensRevokedEvent @event, CancellationToken cancellationToken = default)
    {
        return auditTrailStore.RecordAsync(new ManagementAuditEntry
        {
            Category = @event.Category,
            Action = @event.Action,
            Actor = @event.Actor,
            Timestamp = @event.Timestamp,
            Details = $"Batch revoked {@event.Count} tokens. Scope: {@event.Scope}."
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(TokensPrunedEvent @event, CancellationToken cancellationToken = default)
    {
        return auditTrailStore.RecordAsync(new ManagementAuditEntry
        {
            Category = @event.Category,
            Action = @event.Action,
            Actor = @event.Actor,
            Timestamp = @event.Timestamp,
            Details = $"Pruned {@event.PrunedCount} expired/revoked tokens."
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(SessionRevokedEvent @event, CancellationToken cancellationToken = default)
    {
        return auditTrailStore.RecordAsync(new ManagementAuditEntry
        {
            Category = @event.Category,
            Action = @event.Action,
            EntityId = @event.SessionId,
            Actor = @event.Actor,
            Timestamp = @event.Timestamp,
            Details = $"Session authorization revoked. SessionId: {@event.SessionId}, UserId: {@event.UserId}, TokensRevoked: {@event.TokensRevoked}."
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task HandleAsync(SessionDeletedEvent @event, CancellationToken cancellationToken = default)
    {
        return auditTrailStore.RecordAsync(new ManagementAuditEntry
        {
            Category = @event.Category,
            Action = @event.Action,
            EntityId = @event.SessionId,
            Actor = @event.Actor,
            Timestamp = @event.Timestamp,
            Details = $"Session authorization '{@event.SessionId}' permanently deleted."
        }, cancellationToken);
    }
}
