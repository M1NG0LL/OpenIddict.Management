using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Management.Constants;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Models;
using OpenIddict.Management.Results;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Mappers;

namespace OpenIddict.Management.Storage.EfCore.Stores;

/// <summary>
/// Entity Framework Core implementation of <see cref="IScopeManagementService"/>.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public class EfCoreScopeManagementStore<TContext, TKey>(
    TContext dbContext,
    TimeProvider timeProvider) : IScopeManagementService
    where TContext : DbContext
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for scopes.
    /// </summary>
    protected virtual DbSet<ManagementScope<TKey>> Scopes => dbContext.Set<ManagementScope<TKey>>();

    /// <inheritdoc/>
    public virtual async Task<Result<ManagedScope>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Result.Failure<ManagedScope>("ValidationError", "Scope ID cannot be null or empty.");
        }

        var entity = await FindByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<ManagedScope>(ManagementError.EntityNotFound(nameof(ManagementScope<TKey>), id));
        }

        return ScopeMapper.ToModel(entity);
    }

    /// <inheritdoc/>
    public virtual async Task<Result<ManagedScope>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<ManagedScope>("ValidationError", "Scope name cannot be null or empty.");
        }

        var entity = await Scopes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name == name, cancellationToken);

        if (entity is null)
        {
            return Result.Failure<ManagedScope>(ManagementError.EntityNotFound(nameof(ManagementScope<TKey>), name));
        }

        return ScopeMapper.ToModel(entity);
    }

    /// <inheritdoc/>
    public virtual async Task<Result<PagedResult<ManagedScope>>> ListAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result.Failure<PagedResult<ManagedScope>>("ValidationError", "PagedRequest cannot be null.");
        }

        var query = Scopes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(s =>
                (s.Name != null && EF.Functions.Like(s.Name, $"%{search}%")) ||
                (s.DisplayName != null && EF.Functions.Like(s.DisplayName, $"%{search}%")) ||
                (s.Description != null && EF.Functions.Like(s.Description, $"%{search}%")));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy switch
        {
            var s when string.Equals(s, ScopeSortProperties.Name, StringComparison.OrdinalIgnoreCase)
                => request.SortDescending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
            var s when string.Equals(s, ScopeSortProperties.DisplayName, StringComparison.OrdinalIgnoreCase)
                => request.SortDescending ? query.OrderByDescending(s => s.DisplayName) : query.OrderBy(s => s.DisplayName),
            _ => request.SortDescending ? query.OrderByDescending(s => s.Id) : query.OrderBy(s => s.Id)
        };

        var skip = (request.PageIndex - 1) * request.PageSize;
        var entities = await query.Skip(skip).Take(request.PageSize).ToListAsync(cancellationToken);

        var models = entities.Select(ScopeMapper.ToModel).ToList();

        return new PagedResult<ManagedScope>
        {
            Items = models,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc/>
    public virtual async Task<Result<ManagedScope>> CreateAsync(
        string name,
        string? displayName,
        string? description,
        IEnumerable<string>? resources = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<ManagedScope>("ValidationError", "Scope name cannot be null or empty.");
        }

        var exists = await Scopes.AnyAsync(s => s.Name == name, cancellationToken);
        if (exists)
        {
            return Result.Failure<ManagedScope>(ManagementError.DuplicateEntity(nameof(ManagementScope<TKey>), nameof(name), name));
        }

        var now = timeProvider.GetUtcNow();
        var entity = Activator.CreateInstance<ManagementScope<TKey>>();
        entity.Name = name;
        entity.DisplayName = displayName;
        entity.Description = description;
        entity.CreatedAt = now;

        if (resources is not null)
        {
            entity.Resources = JsonSerializer.Serialize(resources);
        }

        Scopes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ScopeMapper.ToModel(entity);
    }

    /// <inheritdoc/>
    public virtual async Task<Result<ManagedScope>> UpdateAsync(
        string id,
        string? displayName,
        string? description,
        IEnumerable<string>? resources = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Result.Failure<ManagedScope>("ValidationError", "Scope ID cannot be null or empty.");
        }

        var entity = await FindByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<ManagedScope>(ManagementError.EntityNotFound(nameof(ManagementScope<TKey>), id));
        }

        var now = timeProvider.GetUtcNow();
        entity.DisplayName = displayName;
        entity.Description = description;
        entity.LastModifiedAt = now;

        if (resources is not null)
        {
            entity.Resources = JsonSerializer.Serialize(resources);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ScopeMapper.ToModel(entity);
    }

    /// <inheritdoc/>
    public virtual async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Result.Failure("ValidationError", "Scope ID cannot be null or empty.");
        }

        var entity = await FindByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<ManagedScope>(ManagementError.EntityNotFound(nameof(ManagementScope<TKey>), id));
        }

        Scopes.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Finds a scope entity by its string representation of the ID.
    /// </summary>
    protected virtual async Task<ManagementScope<TKey>?> FindByIdAsync(string id, CancellationToken cancellationToken)
    {
        if (typeof(TKey) == typeof(Guid) && Guid.TryParse(id, out var guid))
        {
            var key = (TKey)(object)guid;
            return await Scopes.FirstOrDefaultAsync(s => s.Id != null && s.Id.Equals(key), cancellationToken);
        }

        return await Scopes.FirstOrDefaultAsync(s => s.Id != null && s.Id.ToString() == id, cancellationToken);
    }
}
