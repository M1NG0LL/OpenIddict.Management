using Microsoft.EntityFrameworkCore;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Events;
using OpenIddict.Management.Results;
using OpenIddict.Management.Storage.EfCore.Entities;

namespace OpenIddict.Management.Storage.EfCore.Stores;

/// <summary>
/// Entity Framework Core implementation of <see cref="IOpenIddictAuthorizationManager"/>.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public class EfCoreAuthorizationStore<TContext, TKey>(
    TContext dbContext,
    TimeProvider timeProvider,
    IManagementEventPublisher? eventPublisher = null) : IOpenIddictAuthorizationManager, IAuthorizationManagementService
    where TContext : DbContext
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for authorizations.
    /// </summary>
    protected virtual DbSet<ManagementAuthorization<TKey>> Authorizations => dbContext.Set<ManagementAuthorization<TKey>>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for tokens.
    /// </summary>
    protected virtual DbSet<ManagementToken<TKey>> Tokens => dbContext.Set<ManagementToken<TKey>>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for applications.
    /// </summary>
    protected virtual DbSet<ManagementApplication<TKey>> Applications => dbContext.Set<ManagementApplication<TKey>>();

    /// <inheritdoc/>
    public virtual async Task<Result<PagedResult<SessionListDto>>> ListSessionsAsync(
        SessionFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        filter ??= new SessionFilterRequest();

        var query = Authorizations.Include(a => a.Application).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.UserId))
        {
            var userId = filter.UserId.Trim();
            query = query.Where(a => a.Subject == userId);
        }

        if (!string.IsNullOrWhiteSpace(filter.ClientId))
        {
            var clientId = filter.ClientId.Trim();
            query = query.Where(a => a.Application != null && a.Application.ClientId == clientId);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim();
            query = query.Where(a =>
                (a.Subject != null && a.Subject.Contains(search)) ||
                (a.Application != null && ((a.Application.ClientId != null && a.Application.ClientId.Contains(search)) || (a.Application.DisplayName != null && a.Application.DisplayName.Contains(search)))) ||
                (a.Scopes != null && a.Scopes.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (string.Equals(filter.Status, "active", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(filter.Status, "valid", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.Status != "revoked");
            }
            else if (string.Equals(filter.Status, "revoked", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.Status == "revoked");
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageIndex = filter.PageIndex < 1 ? 1 : filter.PageIndex;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;
        var sortBy = filter.SortBy?.Trim().ToLowerInvariant();
        query = (sortBy, filter.SortDescending) switch
        {
            ("subject", true) => query.OrderByDescending(a => a.Subject).ThenByDescending(a => a.Id),
            ("subject", false) => query.OrderBy(a => a.Subject).ThenByDescending(a => a.Id),
            ("clientid", true) => query.OrderByDescending(a => a.Application != null ? a.Application.ClientId : null).ThenByDescending(a => a.Id),
            ("clientid", false) => query.OrderBy(a => a.Application != null ? a.Application.ClientId : null).ThenByDescending(a => a.Id),
            ("status", true) => query.OrderByDescending(a => a.Status).ThenByDescending(a => a.Id),
            ("status", false) => query.OrderBy(a => a.Status).ThenByDescending(a => a.Id),
            ("creationdate" or "createdat", false) => query.OrderBy(a => a.CreationDate).ThenBy(a => a.Id),
            _ => query.OrderByDescending(a => a.CreationDate).ThenByDescending(a => a.Id)
        };

        var raw = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                Id = a.Id != null ? a.Id.ToString()! : string.Empty,
                a.Subject,
                ClientId = a.Application != null ? a.Application.ClientId : null,
                ClientDisplayName = a.Application != null ? a.Application.DisplayName : null,
                a.Status,
                a.Scopes,
                a.CreatedAt,
                a.CreationDate,
                a.LastModifiedAt,
                TokenCount = a.Tokens.Count
            })
            .ToListAsync(cancellationToken);

        var items = raw.Select(a => new SessionListDto
        {
            Id = a.Id,
            Subject = a.Subject,
            ClientId = a.ClientId,
            ClientDisplayName = a.ClientDisplayName,
            Status = a.Status,
            Scopes = a.Scopes,
            CreatedAt = a.CreatedAt != default
                ? a.CreatedAt.ToUniversalTime()
                : (a.CreationDate.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(a.CreationDate.Value, DateTimeKind.Utc)) : DateTimeOffset.UtcNow),
            LastModifiedAt = a.LastModifiedAt?.ToUniversalTime(),
            TokenCount = a.TokenCount
        }).ToList();

        return new PagedResult<SessionListDto>
        {
            Items = items,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc/>
    public virtual async Task<Result<int>> GetActiveAuthorizationsCountAsync(CancellationToken cancellationToken = default)
    {
        var count = await Authorizations.AsNoTracking()
            .CountAsync(a => a.Status != "revoked", cancellationToken);

        return count;
    }

    /// <inheritdoc/>
    public virtual async Task<Result<RevocationResultDto>> RevokeSessionAuthorizationsAsync(
        string? userId = null,
        string? authorizationId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(authorizationId))
        {
            return Result.Failure<RevocationResultDto>("ValidationError", "Either User ID or Authorization ID must be provided.");
        }

        var authQuery = Authorizations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            authQuery = authQuery.Where(a => a.Subject == userId);
        }

        if (!string.IsNullOrWhiteSpace(authorizationId))
        {
            if (typeof(TKey) == typeof(Guid) && Guid.TryParse(authorizationId, out var authGuid))
            {
                var key = (TKey)(object)authGuid;
                authQuery = authQuery.Where(a => a.Id != null && a.Id.Equals(key));
            }
            else
            {
                authQuery = authQuery.Where(a => a.Id != null && a.Id.ToString() == authorizationId);
            }
        }

        var activeAuthorizations = await authQuery
            .Where(a => a.Status != "revoked")
            .ToListAsync(cancellationToken);

        var authIds = activeAuthorizations.Select(a => a.Id).ToList();

        var tokens = await Tokens
            .Where(t => t.Authorization != null && t.Authorization.Id != null && authIds.Contains(t.Authorization.Id) && t.Status != "revoked")
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();

        foreach (var auth in activeAuthorizations)
        {
            auth.Status = "revoked";
            auth.LastModifiedAt = now;
        }

        foreach (var token in tokens)
        {
            token.Status = "revoked";
            token.RevokedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (eventPublisher is not null)
        {
            await eventPublisher.PublishAsync(new SessionRevokedEvent(authorizationId, userId, tokens.Count), cancellationToken);
        }

        return new RevocationResultDto
        {
            TokensRevoked = tokens.Count,
            AuthorizationsRevoked = activeAuthorizations.Count,
            Scope = RevocationScope.Session
        };
    }

    /// <inheritdoc/>
    public virtual async Task<Result<SessionDetailsDto>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Result.Failure<SessionDetailsDto>("ValidationError", "Authorization ID cannot be empty.");
        }

        var query = Authorizations
            .Include(a => a.Application)
            .Include(a => a.Tokens)
            .AsNoTracking();

        ManagementAuthorization<TKey>? entity = null;
        if (typeof(TKey) == typeof(Guid) && Guid.TryParse(id, out var guid))
        {
            var key = (TKey)(object)guid;
            entity = await query.FirstOrDefaultAsync(a => a.Id != null && a.Id.Equals(key), cancellationToken);
        }
        else
        {
            entity = await query.FirstOrDefaultAsync(a => a.Id != null && a.Id.ToString() == id, cancellationToken);
        }

        if (entity is null)
        {
            return Result.Failure<SessionDetailsDto>(ManagementError.EntityNotFound(nameof(ManagementAuthorization<TKey>), id));
        }

        var tokens = entity.Tokens.Select(t => new TokenListDto
        {
            Id = t.Id != null ? t.Id.ToString()! : string.Empty,
            ReferenceId = t.ReferenceId,
            Subject = t.Subject,
            ClientId = entity.Application?.ClientId,
            ClientDisplayName = entity.Application?.DisplayName,
            Type = t.Type,
            Status = t.Status,
            CreatedAt = t.CreationDate.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(t.CreationDate.Value, DateTimeKind.Utc)) : null,
            ExpirationDate = t.ExpirationDate.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(t.ExpirationDate.Value, DateTimeKind.Utc)) : null,
            RevokedAt = t.RevokedAt?.ToUniversalTime(),
            Payload = t.Payload,
            Properties = t.Properties
        }).ToList();

        var dto = new SessionDetailsDto
        {
            Id = entity.Id?.ToString() ?? string.Empty,
            Subject = entity.Subject,
            ClientId = entity.Application?.ClientId,
            ClientDisplayName = entity.Application?.DisplayName,
            Status = entity.Status,
            Type = entity.Type,
            Scopes = entity.Scopes,
            CreatedAt = entity.CreatedAt != default
                ? entity.CreatedAt.ToUniversalTime()
                : (entity.CreationDate.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(entity.CreationDate.Value, DateTimeKind.Utc)) : DateTimeOffset.UtcNow),
            LastModifiedAt = entity.LastModifiedAt?.ToUniversalTime(),
            Tokens = tokens
        };

        return dto;
    }

    /// <inheritdoc/>
    public virtual async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Result.Failure("ValidationError", "Authorization ID cannot be empty.");
        }

        ManagementAuthorization<TKey>? entity = null;
        if (typeof(TKey) == typeof(Guid) && Guid.TryParse(id, out var guid))
        {
            var key = (TKey)(object)guid;
            entity = await Authorizations.FirstOrDefaultAsync(a => a.Id != null && a.Id.Equals(key), cancellationToken);
        }
        else
        {
            entity = await Authorizations.FirstOrDefaultAsync(a => a.Id != null && a.Id.ToString() == id, cancellationToken);
        }

        if (entity is null)
        {
            return Result.Failure(ManagementError.EntityNotFound(nameof(ManagementAuthorization<TKey>), id));
        }

        var tokens = await Tokens
            .Where(t => t.Authorization != null && t.Authorization.Id != null && t.Authorization.Id.Equals(entity.Id))
            .ToListAsync(cancellationToken);

        if (tokens.Count > 0)
        {
            Tokens.RemoveRange(tokens);
        }

        Authorizations.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (eventPublisher is not null)
        {
            await eventPublisher.PublishAsync(new SessionDeletedEvent(id), cancellationToken);
        }

        return Result.Success();
    }
}
