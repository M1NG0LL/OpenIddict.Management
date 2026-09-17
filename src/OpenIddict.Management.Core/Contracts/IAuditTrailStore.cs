using OpenIddict.Management.Dto;
using OpenIddict.Management.Models;
using OpenIddict.Management.Results;

namespace OpenIddict.Management.Contracts;

/// <summary>
/// Service contract for storing, querying, and retrieving OpenIddict management audit trail records.
/// </summary>
public interface IAuditTrailStore
{
    /// <summary>
    /// Records a new audit trail entry.
    /// </summary>
    /// <param name="entry">The audit trail entry to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous record operation.</returns>
    Task RecordAsync(ManagementAuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries audit trail entries matching the specified filter criteria.
    /// </summary>
    /// <param name="filter">The audit filter parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a <see cref="PagedResult{T}"/> of <see cref="ManagementAuditEntry"/>.</returns>
    Task<Result<PagedResult<ManagementAuditEntry>>> QueryAsync(AuditFilterRequest filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single audit trail entry by its unique identifier.
    /// </summary>
    /// <param name="id">The audit entry identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the audit entry if found.</returns>
    Task<Result<ManagementAuditEntry>> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}
