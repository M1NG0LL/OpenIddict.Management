using Microsoft.EntityFrameworkCore;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Results;
using OpenIddict.Management.Storage.EfCore.Entities;

namespace OpenIddict.Management.Storage.EfCore.Stores;

/// <summary>
/// Entity Framework Core implementation of <see cref="IOpenIddictRevocationManager"/>.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public class EfCoreRevocationStore<TContext, TKey>(
    TContext dbContext,
    TimeProvider timeProvider) : IOpenIddictRevocationManager
    where TContext : DbContext
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for tokens.
    /// </summary>
    protected virtual DbSet<ManagementToken<TKey>> Tokens => dbContext.Set<ManagementToken<TKey>>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for authorizations.
    /// </summary>
    protected virtual DbSet<ManagementAuthorization<TKey>> Authorizations => dbContext.Set<ManagementAuthorization<TKey>>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for applications.
    /// </summary>
    protected virtual DbSet<ManagementApplication<TKey>> Applications => dbContext.Set<ManagementApplication<TKey>>();

    /// <inheritdoc/>
    public virtual async Task<Result<RevocationResultDto>> RevokeByTokenIdAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
        {
            return Result.Failure<RevocationResultDto>("ValidationError", "Token ID cannot be null or empty.");
        }

        var token = await FindTokenByIdAsync(tokenId, cancellationToken);
        if (token is null)
        {
            return new RevocationResultDto
            {
                TokensRevoked = 0,
                Scope = RevocationScope.Token
            };
        }

        var now = timeProvider.GetUtcNow();
        token.Status = "revoked";
        token.RevokedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RevocationResultDto
        {
            TokensRevoked = 1,
            Scope = RevocationScope.Token
        };
    }

    /// <inheritdoc/>
    public virtual async Task<Result<RevocationResultDto>> RevokeByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result.Failure<RevocationResultDto>("ValidationError", "User ID cannot be null or empty.");
        }

        var tokens = await Tokens
            .Where(t => t.Subject == userId && t.Status != "revoked")
            .ToListAsync(cancellationToken);

        var authorizations = await Authorizations
            .Where(a => a.Subject == userId && a.Status != "revoked")
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();

        foreach (var token in tokens)
        {
            token.Status = "revoked";
            token.RevokedAt = now;
        }

        foreach (var auth in authorizations)
        {
            auth.Status = "revoked";
            auth.LastModifiedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RevocationResultDto
        {
            TokensRevoked = tokens.Count,
            AuthorizationsRevoked = authorizations.Count,
            Scope = RevocationScope.User
        };
    }

    /// <inheritdoc/>
    public virtual async Task<Result<RevocationResultDto>> RevokeByClientAsync(string clientId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Result.Failure<RevocationResultDto>("ValidationError", "Client ID cannot be null or empty.");
        }

        var authorizations = await Authorizations
            .Include(a => a.Application)
            .Where(a => a.Application != null && a.Application.ClientId == clientId && a.Status != "revoked")
            .ToListAsync(cancellationToken);

        var tokens = await Tokens
            .Include(t => t.Application)
            .Where(t => t.Application != null && t.Application.ClientId == clientId && t.Status != "revoked")
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();

        foreach (var token in tokens)
        {
            token.Status = "revoked";
            token.RevokedAt = now;
        }

        foreach (var auth in authorizations)
        {
            auth.Status = "revoked";
            auth.LastModifiedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RevocationResultDto
        {
            TokensRevoked = tokens.Count,
            AuthorizationsRevoked = authorizations.Count,
            Scope = RevocationScope.Client
        };
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

        return new RevocationResultDto
        {
            TokensRevoked = tokens.Count,
            AuthorizationsRevoked = activeAuthorizations.Count,
            Scope = RevocationScope.Session
        };
    }

    /// <inheritdoc/>
    public virtual Task<Result<int>> PruneExpiredTokensAsync(CancellationToken cancellationToken = default)
        => PruneTokensAsync(batchSize: null, includeRevoked: true, cancellationToken);

    /// <inheritdoc/>
    public virtual async Task<Result<int>> PruneTokensAsync(
        int? batchSize = null,
        bool includeRevoked = true,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var query = Tokens.AsQueryable();

        if (includeRevoked)
        {
            query = query.Where(t => (t.ExpirationDate != null && t.ExpirationDate <= nowUtc) || t.Status == "revoked" || t.RevokedAt != null);
        }
        else
        {
            query = query.Where(t => t.ExpirationDate != null && t.ExpirationDate <= nowUtc);
        }

        if (batchSize.HasValue && batchSize.Value > 0)
        {
            query = query.OrderBy(t => t.CreationDate).Take(batchSize.Value);
        }

        var tokensToDelete = await query.ToListAsync(cancellationToken);
        if (tokensToDelete.Count == 0)
        {
            return 0;
        }

        Tokens.RemoveRange(tokensToDelete);
        await dbContext.SaveChangesAsync(cancellationToken);

        return tokensToDelete.Count;
    }

    /// <inheritdoc/>
    public virtual async Task<Result<PagedResult<TokenListDto>>> ListTokensAsync(
        TokenFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        filter ??= new TokenFilterRequest();

        var query = Tokens.AsNoTracking().Include(t => t.Application).AsQueryable();

        query = ApplyTokenFilters(query, filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var pageIndex = filter.PageIndex < 1 ? 1 : filter.PageIndex;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;

        var tokens = await query
            .OrderByDescending(t => t.CreationDate)
            .ThenByDescending(t => t.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                Id = t.Id != null ? t.Id.ToString()! : string.Empty,
                t.ReferenceId,
                t.Subject,
                ClientId = t.Application != null ? t.Application.ClientId : null,
                ClientDisplayName = t.Application != null ? t.Application.DisplayName : null,
                t.Type,
                t.Status,
                t.CreatedAt,
                t.CreationDate,
                t.ExpirationDate,
                t.RevokedAt,
                t.Payload,
                t.Properties
            })
            .ToListAsync(cancellationToken);

        var items = tokens.Select(t => new TokenListDto
        {
            Id = t.Id,
            ReferenceId = t.ReferenceId,
            Subject = t.Subject,
            ClientId = t.ClientId,
            ClientDisplayName = t.ClientDisplayName,
            Type = t.Type,
            CreatedAt = t.CreatedAt != default
                ? t.CreatedAt.ToUniversalTime()
                : (t.CreationDate.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(t.CreationDate.Value, DateTimeKind.Utc)) : null),
            ExpirationDate = t.ExpirationDate.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(t.ExpirationDate.Value, DateTimeKind.Utc))
                : null,
            RevokedAt = t.RevokedAt?.ToUniversalTime(),
            Payload = t.Payload,
            Properties = t.Properties
        }).ToList();

        return new PagedResult<TokenListDto>
        {
            Items = items,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc/>
    public virtual async Task<Result<RevocationResultDto>> RevokeTokensWithFilterAsync(
        TokenFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        filter ??= new TokenFilterRequest();

        var query = Tokens.Include(t => t.Application).AsQueryable();

        query = ApplyTokenFilters(query, filter);

        // Only revoke tokens that are not already revoked
        query = query.Where(t => t.Status != "revoked" && t.RevokedAt == null);

        var tokensToRevoke = await query.ToListAsync(cancellationToken);
        if (tokensToRevoke.Count == 0)
        {
            return new RevocationResultDto
            {
                TokensRevoked = 0,
                Scope = RevocationScope.Token
            };
        }

        var now = timeProvider.GetUtcNow();
        foreach (var token in tokensToRevoke)
        {
            token.Status = "revoked";
            token.RevokedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RevocationResultDto
        {
            TokensRevoked = tokensToRevoke.Count,
            Scope = RevocationScope.Token
        };
    }

    /// <inheritdoc/>
    public virtual async Task<Result<TokenCountSummaryDto>> GetTokenCountsAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var total = await Tokens.AsNoTracking().CountAsync(cancellationToken);
        var revoked = await Tokens.AsNoTracking()
            .CountAsync(t => t.Status == "revoked" || t.RevokedAt != null, cancellationToken);
        var expired = await Tokens.AsNoTracking()
            .CountAsync(t => (t.Status != "revoked" && t.RevokedAt == null) && t.ExpirationDate != null && t.ExpirationDate <= nowUtc, cancellationToken);
        var valid = Math.Max(0, total - revoked - expired);

        return new TokenCountSummaryDto
        {
            Total = total,
            Valid = valid,
            Expired = expired,
            Revoked = revoked
        };
    }

    /// <inheritdoc/>
    public virtual async Task<Result<int>> GetActiveAuthorizationsCountAsync(CancellationToken cancellationToken = default)
    {
        var count = await Authorizations.AsNoTracking()
            .CountAsync(a => a.Status != "revoked", cancellationToken);

        return count;
    }

    /// <summary>
    /// Applies filter criteria to a token queryable.
    /// </summary>
    protected virtual IQueryable<ManagementToken<TKey>> ApplyTokenFilters(
        IQueryable<ManagementToken<TKey>> query,
        TokenFilterRequest filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.UserId))
        {
            var userId = filter.UserId.Trim();
            query = query.Where(t => t.Subject == userId);
        }

        if (!string.IsNullOrWhiteSpace(filter.ClientId))
        {
            var clientId = filter.ClientId.Trim();
            query = query.Where(t => t.Application != null && t.Application.ClientId == clientId);
        }

        if (!string.IsNullOrWhiteSpace(filter.AuthorizationId))
        {
            var authId = filter.AuthorizationId.Trim();
            if (typeof(TKey) == typeof(Guid) && Guid.TryParse(authId, out var authGuid))
            {
                var key = (TKey)(object)authGuid;
                query = query.Where(t => t.Authorization != null && t.Authorization.Id != null && t.Authorization.Id.Equals(key));
            }
            else
            {
                query = query.Where(t => t.Authorization != null && t.Authorization.Id != null && t.Authorization.Id.ToString() == authId);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim();
            query = query.Where(t =>
                (t.Subject != null && t.Subject.Contains(search)) ||
                (t.ReferenceId != null && t.ReferenceId.Contains(search)) ||
                (t.Application != null && ((t.Application.ClientId != null && t.Application.ClientId.Contains(search)) || (t.Application.DisplayName != null && t.Application.DisplayName.Contains(search)))) ||
                (t.Type != null && t.Type.Contains(search)));
        }

        if (filter.CreatedFrom.HasValue)
        {
            var from = filter.CreatedFrom.Value;
            var fromUtc = from.UtcDateTime;
            query = query.Where(t => t.CreatedAt >= from || (t.CreationDate != null && t.CreationDate >= fromUtc));
        }

        if (filter.CreatedTo.HasValue)
        {
            var to = filter.CreatedTo.Value;
            var toUtc = to.UtcDateTime;
            query = query.Where(t => t.CreatedAt <= to || (t.CreationDate != null && t.CreationDate <= toUtc));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            if (string.Equals(filter.Status, "active", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(filter.Status, "valid", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.Status != "revoked" && t.RevokedAt == null && (t.ExpirationDate == null || t.ExpirationDate > nowUtc));
            }
            else if (string.Equals(filter.Status, "expired", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.Status != "revoked" && t.RevokedAt == null && t.ExpirationDate != null && t.ExpirationDate <= nowUtc);
            }
            else if (string.Equals(filter.Status, "revoked", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.Status == "revoked" || t.RevokedAt != null);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.TokenType))
        {
            query = query.Where(t => t.Type == filter.TokenType);
        }

        return query;
    }

    /// <summary>
    /// Finds a token entity by its string representation of the ID.
    /// </summary>
    protected virtual async Task<ManagementToken<TKey>?> FindTokenByIdAsync(string tokenId, CancellationToken cancellationToken)
    {
        if (typeof(TKey) == typeof(Guid) && Guid.TryParse(tokenId, out var guid))
        {
            var key = (TKey)(object)guid;
            return await Tokens.FirstOrDefaultAsync(t => t.Id != null && t.Id.Equals(key), cancellationToken);
        }

        return await Tokens.FirstOrDefaultAsync(t => t.Id != null && t.Id.ToString() == tokenId, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<Result<List<ApplicationTokenCountDto>>> GetTokenCountsByApplicationAsync(CancellationToken cancellationToken = default)
    {
        var appCounts = await Applications.AsNoTracking()
            .Select(a => new ApplicationTokenCountDto
            {
                ApplicationId = a.Id != null ? a.Id.ToString()! : string.Empty,
                ClientId = a.ClientId ?? string.Empty,
                DisplayName = a.DisplayName,
                TokenCount = a.Tokens.Count
            })
            .OrderByDescending(x => x.TokenCount)
            .ToListAsync(cancellationToken);

        return appCounts;
    }

    /// <inheritdoc/>
    public virtual async Task<Result<List<TokenTimelineDataPointDto>>> GetTokenTimelineAsync(
        DateOnly from,
        DateOnly to,
        string? clientId = null,
        CancellationToken cancellationToken = default)
    {
        var startDateTime = from.ToDateTime(TimeOnly.MinValue);
        var endDateTime = to.ToDateTime(TimeOnly.MaxValue);

        var tokenQuery = Tokens.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(clientId))
        {
            var trimmed = clientId.Trim();
            tokenQuery = tokenQuery.Where(t => t.Application != null && t.Application.ClientId == trimmed);
        }

        var rawDates = await tokenQuery
            .Where(t => t.CreationDate != null && t.CreationDate >= startDateTime && t.CreationDate <= endDateTime)
            .Select(t => t.CreationDate!.Value)
            .ToListAsync(cancellationToken);

        var countsByDate = rawDates
            .GroupBy(d => DateOnly.FromDateTime(d.Date))
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new List<TokenTimelineDataPointDto>();
        for (var cur = from; cur <= to; cur = cur.AddDays(1))
        {
            result.Add(new TokenTimelineDataPointDto
            {
                Date = cur,
                Count = countsByDate.GetValueOrDefault(cur, 0)
            });
        }

        return result;
    }

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

        var raw = await query
            .OrderByDescending(a => a.CreationDate)
            .ThenByDescending(a => a.Id)
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
}
