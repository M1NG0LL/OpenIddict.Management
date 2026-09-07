using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Management.Constants;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Models;
using OpenIddict.Management.Results;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Mappers;

namespace OpenIddict.Management.Storage.EfCore.Stores;

/// <summary>
/// Entity Framework Core implementation of <see cref="IApplicationManagementService"/>.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public class EfCoreApplicationManagementStore<TContext, TKey>(
    TContext dbContext,
    TimeProvider timeProvider,
    IOpenIddictApplicationManager? applicationManager = null) : IApplicationManagementService
    where TContext : DbContext
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for applications.
    /// </summary>
    protected virtual DbSet<ManagementApplication<TKey>> Applications => dbContext.Set<ManagementApplication<TKey>>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for authorizations.
    /// </summary>
    protected virtual DbSet<ManagementAuthorization<TKey>> Authorizations => dbContext.Set<ManagementAuthorization<TKey>>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for tokens.
    /// </summary>
    protected virtual DbSet<ManagementToken<TKey>> Tokens => dbContext.Set<ManagementToken<TKey>>();

    /// <inheritdoc/>
    public virtual async Task<Result<ManagedApplication>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Result.Failure<ManagedApplication>("ValidationError", "Application ID cannot be null or empty.");
        }

        var entity = await FindByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<ManagedApplication>(ManagementError.EntityNotFound(nameof(ManagementApplication<TKey>), id));
        }

        return ApplicationMapper.ToModel(entity);
    }

    /// <inheritdoc/>
    public virtual async Task<Result<ManagedApplication>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Result.Failure<ManagedApplication>("ValidationError", "Client ID cannot be null or empty.");
        }

        var entity = await Applications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.ClientId == clientId, cancellationToken);

        if (entity is null)
        {
            return Result.Failure<ManagedApplication>(ManagementError.EntityNotFound(nameof(ManagementApplication<TKey>), clientId));
        }

        return ApplicationMapper.ToModel(entity);
    }

    /// <inheritdoc/>
    public virtual async Task<Result<PagedResult<ApplicationListDto>>> ListAsync(
        PagedRequest request,
        ApplicationStatus? statusFilter = null,
        ApplicationEnvironment? environmentFilter = null,
        List<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result.Failure<PagedResult<ApplicationListDto>>("ValidationError", "PagedRequest cannot be null.");
        }

        var query = Applications.AsNoTracking().AsQueryable();

        if (statusFilter.HasValue)
        {
            query = query.Where(a => a.Status == statusFilter.Value);
        }
        else
        {
            query = query.Where(a => a.Status != ApplicationStatus.Deleted);
        }

        if (environmentFilter.HasValue)
        {
            query = query.Where(a => a.Environment == environmentFilter.Value);
        }

        if (tags is not null && tags.Count > 0)
        {
            foreach (var tag in tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    var trimmed = tag.Trim();
                    query = query.Where(a => a.Tags.Contains(trimmed));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(a =>
                (a.ClientId != null && EF.Functions.Like(a.ClientId, $"%{search}%")) ||
                (a.DisplayName != null && EF.Functions.Like(a.DisplayName, $"%{search}%")) ||
                (a.OwnerUserId != null && EF.Functions.Like(a.OwnerUserId, $"%{search}%")));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy switch
        {
            var s when string.Equals(s, ApplicationSortProperties.ClientId, StringComparison.OrdinalIgnoreCase)
                => request.SortDescending ? query.OrderByDescending(a => a.ClientId) : query.OrderBy(a => a.ClientId),
            var s when string.Equals(s, ApplicationSortProperties.DisplayName, StringComparison.OrdinalIgnoreCase)
                => request.SortDescending ? query.OrderByDescending(a => a.DisplayName) : query.OrderBy(a => a.DisplayName),
            var s when string.Equals(s, ApplicationSortProperties.Status, StringComparison.OrdinalIgnoreCase)
                => request.SortDescending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            var s when string.Equals(s, ApplicationSortProperties.Environment, StringComparison.OrdinalIgnoreCase)
                => request.SortDescending ? query.OrderByDescending(a => a.Environment) : query.OrderBy(a => a.Environment),
            var s when string.Equals(s, ApplicationSortProperties.OwnerUserId, StringComparison.OrdinalIgnoreCase)
                => request.SortDescending ? query.OrderByDescending(a => a.OwnerUserId) : query.OrderBy(a => a.OwnerUserId),
            _ => request.SortDescending ? query.OrderByDescending(a => a.Id) : query.OrderBy(a => a.Id)
        };

        var skip = (request.PageIndex - 1) * request.PageSize;
        var entities = await query.Skip(skip).Take(request.PageSize).ToListAsync(cancellationToken);

        var dtos = entities.Select(ApplicationMapper.ToListDto).ToList();

        return new PagedResult<ApplicationListDto>
        {
            Items = dtos,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc/>
    public virtual async Task<Result<ManagedApplication>> CreateAsync(ApplicationCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (dto is null)
        {
            return Result.Failure<ManagedApplication>("ValidationError", "ApplicationCreateDto cannot be null.");
        }
        if (string.IsNullOrWhiteSpace(dto.ClientId))
        {
            return Result.Failure<ManagedApplication>("ValidationError", "ClientId cannot be null or empty.");
        }

        var exists = await Applications.AnyAsync(a => a.ClientId == dto.ClientId, cancellationToken);
        if (exists)
        {
            return Result.Failure<ManagedApplication>(ManagementError.DuplicateEntity(nameof(ManagementApplication<TKey>), nameof(dto.ClientId), dto.ClientId));
        }

        var now = timeProvider.GetUtcNow();
        var entity = Activator.CreateInstance<ManagementApplication<TKey>>();
        entity.ClientId = dto.ClientId;
        entity.DisplayName = dto.DisplayName;
        entity.Status = ApplicationStatus.Active;
        entity.Environment = dto.Environment;
        entity.Description = dto.Description;
        entity.LogoUri = dto.LogoUri;
        entity.OwnerUserId = dto.OwnerUserId;
        entity.ExtraData = dto.ExtraData;
        entity.Tags = dto.Tags?.ToList() ?? [];
        entity.CreatedAt = now;
        entity.SetAllowedRoles(dto.AllowedRoles);
        entity.RedirectUris = FilterValidAbsoluteUris(dto.RedirectUris);
        entity.PostLogoutRedirectUris = FilterValidAbsoluteUris(dto.PostLogoutRedirectUris);
        entity.Permissions = JsonSerializer.Serialize(dto.Permissions);
        entity.Requirements = JsonSerializer.Serialize(dto.Requirements);

        if (!string.IsNullOrWhiteSpace(dto.ClientSecret))
        {
            if (applicationManager is null)
            {
                throw new InvalidOperationException("IOpenIddictApplicationManager is required to hash and store client secrets securely. Ensure OpenIddict core services are registered in Dependency Injection.");
            }

            entity.ClientType = OpenIddictConstants.ClientTypes.Confidential;
            await applicationManager.CreateAsync(entity, dto.ClientSecret, cancellationToken);
        }
        else
        {
            entity.ClientType = OpenIddictConstants.ClientTypes.Public;
            entity.ClientSecret = null;
            Applications.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ApplicationMapper.ToModel(entity);
    }

    /// <inheritdoc/>
    public virtual async Task<Result<ManagedApplication>> UpdateAsync(string id, ApplicationUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Result.Failure<ManagedApplication>("ValidationError", "Application ID cannot be null or empty.");
        }
        if (dto is null)
        {
            return Result.Failure<ManagedApplication>("ValidationError", "ApplicationUpdateDto cannot be null.");
        }

        var entity = await FindByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<ManagedApplication>(ManagementError.EntityNotFound(nameof(ManagementApplication<TKey>), id));
        }

        var now = timeProvider.GetUtcNow();
        entity.DisplayName = dto.DisplayName;
        entity.Status = dto.Status;
        entity.Environment = dto.Environment;
        entity.Description = dto.Description;
        entity.LogoUri = dto.LogoUri;
        entity.OwnerUserId = dto.OwnerUserId;
        entity.ExtraData = dto.ExtraData;
        entity.Tags = dto.Tags?.ToList() ?? [];
        entity.LastModifiedAt = now;
        entity.SetAllowedRoles(dto.AllowedRoles);
        entity.RedirectUris = FilterValidAbsoluteUris(dto.RedirectUris);
        entity.PostLogoutRedirectUris = FilterValidAbsoluteUris(dto.PostLogoutRedirectUris);
        entity.Permissions = JsonSerializer.Serialize(dto.Permissions);
        entity.Requirements = JsonSerializer.Serialize(dto.Requirements);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApplicationMapper.ToModel(entity);
    }

    /// <inheritdoc/>
    public virtual async Task<Result> UpdateClientSecretAsync(string id, string? newClientSecret, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Result.Failure("ValidationError", "Application ID cannot be null or empty.");
        }

        var entity = await FindByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure(ManagementError.EntityNotFound(nameof(ManagementApplication<TKey>), id));
        }

        entity.LastModifiedAt = timeProvider.GetUtcNow();

        // Sanitize existing URIs in case legacy data contains invalid relative URIs
        SanitizeEntityUris(entity);

        if (!string.IsNullOrWhiteSpace(newClientSecret))
        {
            if (applicationManager is null)
            {
                throw new InvalidOperationException("IOpenIddictApplicationManager is required to hash and store client secrets securely. Ensure OpenIddict core services are registered in Dependency Injection.");
            }

            entity.ClientType = OpenIddictConstants.ClientTypes.Confidential;
            await applicationManager.UpdateAsync(entity, newClientSecret, cancellationToken);
        }
        else
        {
            entity.ClientType = OpenIddictConstants.ClientTypes.Public;
            entity.ClientSecret = null;
            if (applicationManager is not null)
            {
                await applicationManager.UpdateAsync(entity, cancellationToken);
            }
            else
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return Result.Success();
    }

    /// <inheritdoc/>
    public virtual async Task<Result> DeleteAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Result.Failure("ValidationError", "Application ID cannot be null or empty.");
        }

        var entity = await FindByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure(ManagementError.EntityNotFound(nameof(ManagementApplication<TKey>), id));
        }

        // Delete all tokens and authorizations associated with this application
        var authorizations = await Authorizations
            .Where(a => a.Application != null && a.Application.ClientId == entity.ClientId)
            .ToListAsync(cancellationToken);

        var tokens = await Tokens
            .Where(t => (t.Application != null && t.Application.ClientId == entity.ClientId) ||
                        (t.Authorization != null && t.Authorization.Application != null && t.Authorization.Application.ClientId == entity.ClientId))
            .ToListAsync(cancellationToken);

        if (tokens.Count > 0)
        {
            Tokens.RemoveRange(tokens);
        }

        if (authorizations.Count > 0)
        {
            Authorizations.RemoveRange(authorizations);
        }

        if (hardDelete)
        {
            Applications.Remove(entity);
        }
        else
        {
            entity.Status = ApplicationStatus.Deleted;
            entity.LastModifiedAt = timeProvider.GetUtcNow();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc/>
    public virtual async Task<Result> UpdateStatusAsync(string id, ApplicationStatus status, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Result.Failure("ValidationError", "Application ID cannot be null or empty.");
        }

        var entity = await FindByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure(ManagementError.EntityNotFound(nameof(ManagementApplication<TKey>), id));
        }

        entity.Status = status;
        entity.LastModifiedAt = timeProvider.GetUtcNow();

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc/>
    public virtual async Task<Result<int>> SetStatusByEnvironmentAsync(
        ApplicationEnvironment? environment,
        ApplicationStatus status,
        CancellationToken cancellationToken = default)
    {
        var query = Applications.AsQueryable();
        if (environment.HasValue)
        {
            query = query.Where(a => a.Environment == environment.Value);
        }

        var entities = await query.ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        foreach (var entity in entities)
        {
            entity.Status = status;
            entity.LastModifiedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return entities.Count;
    }

    /// <summary>
    /// Finds an application entity by its string representation of the ID.
    /// </summary>
    protected virtual async Task<ManagementApplication<TKey>?> FindByIdAsync(string id, CancellationToken cancellationToken)
    {
        if (typeof(TKey) == typeof(Guid) && Guid.TryParse(id, out var guid))
        {
            var key = (TKey)(object)guid;
            return await Applications.FirstOrDefaultAsync(a => a.Id != null && a.Id.Equals(key), cancellationToken);
        }

        return await Applications.FirstOrDefaultAsync(a => a.Id != null && a.Id.ToString() == id, cancellationToken);
    }

    private static void SanitizeEntityUris(ManagementApplication<TKey> entity)
    {
        entity.RedirectUris = FilterValidAbsoluteUris(entity.RedirectUris);
        entity.PostLogoutRedirectUris = FilterValidAbsoluteUris(entity.PostLogoutRedirectUris);
    }

    private static string FilterValidAbsoluteUris(IEnumerable<string>? uris)
    {
        if (uris is null) return "[]";
        var valid = uris.Where(u => !string.IsNullOrWhiteSpace(u) && Uri.TryCreate(u.Trim(), UriKind.Absolute, out _))
                        .Select(u => u.Trim())
                        .Distinct()
                        .ToList();
        return JsonSerializer.Serialize(valid);
    }

    private static string FilterValidAbsoluteUris(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "[]";
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return FilterValidAbsoluteUris(list);
        }
        catch
        {
            return "[]";
        }
    }
}
