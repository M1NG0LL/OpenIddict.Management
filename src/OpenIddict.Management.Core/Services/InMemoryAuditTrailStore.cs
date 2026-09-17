using System.Collections.Concurrent;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Models;
using OpenIddict.Management.Results;

namespace OpenIddict.Management.Services;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IAuditTrailStore"/>.
/// Provides zero-configuration audit trail tracking out of the box.
/// </summary>
public sealed class InMemoryAuditTrailStore : IAuditTrailStore
{
    private readonly ConcurrentBag<ManagementAuditEntry> _entries = new();
    private const int MaxEntries = 10_000;

    /// <inheritdoc/>
    public Task RecordAsync(ManagementAuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _entries.Add(entry);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<Result<PagedResult<ManagementAuditEntry>>> QueryAsync(
        AuditFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        filter ??= new AuditFilterRequest();

        var query = _entries.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            query = query.Where(e => string.Equals(e.Category, filter.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(e => string.Equals(e.Action, filter.Action, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityId))
        {
            query = query.Where(e => string.Equals(e.EntityId, filter.EntityId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Actor))
        {
            query = query.Where(e => e.Actor != null && e.Actor.Contains(filter.Actor, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.Success.HasValue)
        {
            query = query.Where(e => e.Success == filter.Success.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(e => e.Timestamp >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(e => e.Timestamp <= filter.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(e =>
                (e.Category != null && e.Category.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (e.Action != null && e.Action.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (e.EntityName != null && e.EntityName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (e.EntityId != null && e.EntityId.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (e.Actor != null && e.Actor.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (e.Details != null && e.Details.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        var totalCount = query.Count();

        var sortBy = filter.SortBy?.Trim().ToLowerInvariant();
        query = (sortBy, filter.SortDescending) switch
        {
            ("category", true) => query.OrderByDescending(e => e.Category).ThenByDescending(e => e.Timestamp),
            ("category", false) => query.OrderBy(e => e.Category).ThenByDescending(e => e.Timestamp),
            ("action", true) => query.OrderByDescending(e => e.Action).ThenByDescending(e => e.Timestamp),
            ("action", false) => query.OrderBy(e => e.Action).ThenByDescending(e => e.Timestamp),
            ("entityname", true) => query.OrderByDescending(e => e.EntityName).ThenByDescending(e => e.Timestamp),
            ("entityname", false) => query.OrderBy(e => e.EntityName).ThenByDescending(e => e.Timestamp),
            ("actor", true) => query.OrderByDescending(e => e.Actor).ThenByDescending(e => e.Timestamp),
            ("actor", false) => query.OrderBy(e => e.Actor).ThenByDescending(e => e.Timestamp),
            (_, false) => query.OrderBy(e => e.Timestamp),
            _ => query.OrderByDescending(e => e.Timestamp)
        };

        var pageIndex = filter.PageIndex < 1 ? 1 : filter.PageIndex;
        var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize;

        var items = query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var pagedResult = new PagedResult<ManagementAuditEntry>
        {
            Items = items,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Task.FromResult(Result.Success(pagedResult));
    }

    /// <inheritdoc/>
    public Task<Result<ManagementAuditEntry>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult(Result.Failure<ManagementAuditEntry>("ValidationError", "Audit ID cannot be empty."));
        }

        var entry = _entries.FirstOrDefault(e => string.Equals(e.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            return Task.FromResult(Result.Failure<ManagementAuditEntry>("EntityNotFound", $"Audit entry with ID '{id}' was not found."));
        }

        return Task.FromResult(Result.Success(entry));
    }
}
