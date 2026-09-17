using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Events;
using OpenIddict.Management.Results;
using OpenIddict.Management.Storage.EfCore.Entities;

namespace OpenIddict.Management.Storage.EfCore.Stores;

/// <summary>
/// Entity Framework Core implementation of <see cref="IOpenIddictTokenManager"/>.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public class EfCoreTokenStore<TContext, TKey>(
    TContext dbContext,
    TimeProvider timeProvider,
    IManagementEventPublisher? eventPublisher = null) : IOpenIddictTokenManager, ITokenManagementService
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

        var sortBy = filter.SortBy?.Trim().ToLowerInvariant();
        query = (sortBy, filter.SortDescending) switch
        {
            ("expirationdate", true) => query.OrderByDescending(t => t.ExpirationDate).ThenByDescending(t => t.Id),
            ("expirationdate", false) => query.OrderBy(t => t.ExpirationDate).ThenByDescending(t => t.Id),
            ("subject", true) => query.OrderByDescending(t => t.Subject).ThenByDescending(t => t.Id),
            ("subject", false) => query.OrderBy(t => t.Subject).ThenByDescending(t => t.Id),
            ("clientid", true) => query.OrderByDescending(t => t.Application != null ? t.Application.ClientId : null).ThenByDescending(t => t.Id),
            ("clientid", false) => query.OrderBy(t => t.Application != null ? t.Application.ClientId : null).ThenByDescending(t => t.Id),
            ("type", true) => query.OrderByDescending(t => t.Type).ThenByDescending(t => t.Id),
            ("type", false) => query.OrderBy(t => t.Type).ThenByDescending(t => t.Id),
            ("status", true) => query.OrderByDescending(t => t.Status).ThenByDescending(t => t.Id),
            ("status", false) => query.OrderBy(t => t.Status).ThenByDescending(t => t.Id),
            ("creationdate" or "createdat", false) => query.OrderBy(t => t.CreationDate).ThenBy(t => t.Id),
            _ => query.OrderByDescending(t => t.CreationDate).ThenByDescending(t => t.Id)
        };

        var tokens = await query
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
            CreatedAt = t.CreationDate.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(t.CreationDate.Value, DateTimeKind.Utc)) : null,
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
    public virtual async Task<Result<int>> ExtendTokenExpirationAsync(
        IReadOnlyList<string> tokenIds,
        int additionalMinutes,
        CancellationToken cancellationToken = default)
    {
        if (tokenIds is null || tokenIds.Count == 0)
        {
            return Result.Failure<int>("ValidationError", "At least one token ID must be provided.");
        }

        if (additionalMinutes <= 0)
        {
            return Result.Failure<int>("ValidationError", "Additional minutes must be greater than zero.");
        }

        var tokens = await FindTokensByIdsAsync(tokenIds, cancellationToken);
        var eligibleTokens = tokens.Where(t => t.Status != "revoked" && t.RevokedAt == null).ToList();

        if (eligibleTokens.Count == 0)
        {
            return Result.Failure<int>("NotFound", "No eligible unrevoked tokens were found to extend.");
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var token in eligibleTokens)
        {
            var baseTime = token.ExpirationDate.HasValue && token.ExpirationDate.Value > nowUtc
                ? token.ExpirationDate.Value
                : nowUtc;

            token.ExpirationDate = baseTime.AddMinutes(additionalMinutes);
            if (token.Status == "expired" || token.Status == "inactive")
            {
                token.Status = "valid";
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return eligibleTokens.Count;
    }

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

        if (eventPublisher is not null)
        {
            await eventPublisher.PublishAsync(new TokenRevokedEvent(tokenId, token.Subject, token.Application?.ClientId), cancellationToken);
        }

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

        if (eventPublisher is not null)
        {
            await eventPublisher.PublishAsync(new TokensRevokedEvent(tokens.Count, Scope: "User"), cancellationToken);
        }

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
    public virtual async Task<Result<RevocationResultDto>> RevokeMultipleTokensAsync(
        IReadOnlyList<string> tokenIds,
        CancellationToken cancellationToken = default)
    {
        if (tokenIds is null || tokenIds.Count == 0)
        {
            return new RevocationResultDto
            {
                TokensRevoked = 0,
                Scope = RevocationScope.Token
            };
        }

        var tokens = await FindTokensByIdsAsync(tokenIds, cancellationToken);
        var activeTokens = tokens.Where(t => t.Status != "revoked" && t.RevokedAt == null).ToList();

        if (activeTokens.Count == 0)
        {
            return new RevocationResultDto
            {
                TokensRevoked = 0,
                Scope = RevocationScope.Token
            };
        }

        var now = timeProvider.GetUtcNow();
        foreach (var token in activeTokens)
        {
            token.Status = "revoked";
            token.RevokedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RevocationResultDto
        {
            TokensRevoked = activeTokens.Count,
            Scope = RevocationScope.Token
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

        if (eventPublisher is not null && tokensToDelete.Count > 0)
        {
            await eventPublisher.PublishAsync(new TokensPrunedEvent(tokensToDelete.Count), cancellationToken);
        }

        return tokensToDelete.Count;
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
            var fromUtc = filter.CreatedFrom.Value.UtcDateTime;
            query = query.Where(t => t.CreationDate != null && t.CreationDate >= fromUtc);
        }

        if (filter.CreatedTo.HasValue)
        {
            var toUtc = filter.CreatedTo.Value.UtcDateTime;
            query = query.Where(t => t.CreationDate != null && t.CreationDate <= toUtc);
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

    /// <summary>
    /// Finds token entities matching the provided string IDs.
    /// </summary>
    protected virtual async Task<List<ManagementToken<TKey>>> FindTokensByIdsAsync(
        IEnumerable<string> tokenIds,
        CancellationToken cancellationToken)
    {
        var distinctIds = tokenIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        if (distinctIds.Count == 0)
        {
            return [];
        }

        if (typeof(TKey) == typeof(Guid))
        {
            var guids = new List<TKey>();
            foreach (var id in distinctIds)
            {
                if (Guid.TryParse(id, out var g))
                {
                    guids.Add((TKey)(object)g);
                }
            }

            if (guids.Count == 0)
            {
                return [];
            }

            return await Tokens
                .Where(t => t.Id != null && guids.Contains(t.Id))
                .ToListAsync(cancellationToken);
        }

        if (typeof(TKey) == typeof(string))
        {
            var keys = distinctIds.Cast<TKey>().ToList();
            return await Tokens
                .Where(t => t.Id != null && keys.Contains(t.Id))
                .ToListAsync(cancellationToken);
        }

        var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(TKey));
        var convertedKeys = new List<TKey>();
        foreach (var id in distinctIds)
        {
            try
            {
                if (converter.CanConvertFrom(typeof(string)))
                {
                    var converted = (TKey?)converter.ConvertFromString(id);
                    if (converted is not null)
                    {
                        convertedKeys.Add(converted);
                    }
                }
            }
            catch
            {
                // Ignore unconvertible IDs
            }
        }

        if (convertedKeys.Count == 0)
        {
            return [];
        }

        return await Tokens
            .Where(t => t.Id != null && convertedKeys.Contains(t.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<Result<TokenIntrospectionDto>> IntrospectTokenAsync(
        string idOrReferenceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idOrReferenceId))
        {
            return Result.Failure<TokenIntrospectionDto>("ValidationError", "Token identifier cannot be empty.");
        }

        var trimmed = idOrReferenceId.Trim();
        var query = Tokens.Include(t => t.Application).Include(t => t.Authorization).AsNoTracking();

        ManagementToken<TKey>? entity = null;
        if (typeof(TKey) == typeof(Guid) && Guid.TryParse(trimmed, out var guid))
        {
            var key = (TKey)(object)guid;
            entity = await query.FirstOrDefaultAsync(t => (t.Id != null && t.Id.Equals(key)) || t.ReferenceId == trimmed, cancellationToken);
        }
        else
        {
            entity = await query.FirstOrDefaultAsync(t => (t.Id != null && t.Id.ToString() == trimmed) || t.ReferenceId == trimmed, cancellationToken);
        }

        if (entity is null)
        {
            return Result.Failure<TokenIntrospectionDto>(ManagementError.EntityNotFound(nameof(ManagementToken<TKey>), trimmed));
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var isRevoked = entity.Status == "revoked" || entity.RevokedAt != null;
        var isExpired = entity.ExpirationDate != null && entity.ExpirationDate <= nowUtc;
        var isActive = !isRevoked && !isExpired;

        var claims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var scopes = new List<string>();

        if (entity.Authorization != null && !string.IsNullOrWhiteSpace(entity.Authorization.Scopes))
        {
            try
            {
                var parsedScopes = JsonSerializer.Deserialize<List<string>>(entity.Authorization.Scopes);
                if (parsedScopes is not null) scopes.AddRange(parsedScopes);
            }
            catch
            {
                scopes.AddRange(entity.Authorization.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }
        }

        if (!string.IsNullOrWhiteSpace(entity.Payload))
        {
            try
            {
                using var doc = JsonDocument.Parse(entity.Payload);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array && prop.NameEquals("scope"))
                        {
                            foreach (var s in prop.Value.EnumerateArray())
                            {
                                var val = s.GetString();
                                if (!string.IsNullOrWhiteSpace(val) && !scopes.Contains(val))
                                {
                                    scopes.Add(val);
                                }
                            }
                        }
                        else
                        {
                            claims[prop.Name] = prop.Value.ToString();
                        }
                    }
                }
            }
            catch
            {
                claims["payload_format"] = "encrypted_or_opaque";
            }
        }

        if (!string.IsNullOrWhiteSpace(entity.Properties))
        {
            try
            {
                using var doc = JsonDocument.Parse(entity.Properties);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        claims[$"prop:{prop.Name}"] = prop.Value.ToString();
                    }
                }
            }
            catch
            {
                claims["properties_raw"] = entity.Properties;
            }
        }

        if (!string.IsNullOrWhiteSpace(entity.Subject) && !claims.ContainsKey("sub"))
        {
            claims["sub"] = entity.Subject;
        }

        if (entity.Application?.ClientId is not null && !claims.ContainsKey("client_id"))
        {
            claims["client_id"] = entity.Application.ClientId;
        }

        var dto = new TokenIntrospectionDto
        {
            Active = isActive,
            TokenId = entity.Id?.ToString() ?? string.Empty,
            ReferenceId = entity.ReferenceId,
            TokenType = entity.Type,
            Subject = entity.Subject,
            ClientId = entity.Application?.ClientId,
            ClientDisplayName = entity.Application?.DisplayName,
            IssuedAt = entity.CreationDate.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(entity.CreationDate.Value, DateTimeKind.Utc)) : null,
            ExpiresAt = entity.ExpirationDate.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(entity.ExpirationDate.Value, DateTimeKind.Utc)) : null,
            RevokedAt = entity.RevokedAt?.ToUniversalTime(),
            Status = isRevoked ? "revoked" : (isExpired ? "expired" : "valid"),
            Scopes = scopes.Distinct().ToList(),
            Claims = claims
        };

        return dto;
    }
}
