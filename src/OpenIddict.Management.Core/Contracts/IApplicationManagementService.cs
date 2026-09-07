using OpenIddict.Management.Dto;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Models;
using OpenIddict.Management.Results;

namespace OpenIddict.Management.Contracts;

/// <summary>
/// Service contract for managing OpenIddict application domain models and CRUD operations.
/// </summary>
public interface IApplicationManagementService
{
    /// <summary>
    /// Retrieves a managed application by its unique identifier.
    /// </summary>
    /// <param name="id">The application unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the <see cref="ManagedApplication"/> if found.</returns>
    Task<Result<ManagedApplication>> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a managed application by its client identifier.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the <see cref="ManagedApplication"/> if found.</returns>
    Task<Result<ManagedApplication>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of applications matching optional filters.
    /// </summary>
    /// <param name="request">Pagination and search request parameters.</param>
    /// <param name="statusFilter">Optional status filter.</param>
    /// <param name="environmentFilter">Optional environment filter.</param>
    /// <param name="tags">Optional tags filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a <see cref="PagedResult{T}"/> of <see cref="ApplicationListDto"/> items.</returns>
    Task<Result<PagedResult<ApplicationListDto>>> ListAsync(
        PagedRequest request,
        ApplicationStatus? statusFilter = null,
        ApplicationEnvironment? environmentFilter = null,
        List<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new managed application.
    /// </summary>
    /// <param name="dto">The application creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the created <see cref="ManagedApplication"/>.</returns>
    Task<Result<ManagedApplication>> CreateAsync(ApplicationCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing managed application.
    /// </summary>
    /// <param name="id">The application unique identifier.</param>
    /// <param name="dto">The application update details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the updated <see cref="ManagedApplication"/>.</returns>
    Task<Result<ManagedApplication>> UpdateAsync(string id, ApplicationUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates only the client secret of an existing application.
    /// </summary>
    /// <param name="id">The application unique identifier.</param>
    /// <param name="newClientSecret">The new client secret (or null to clear secret for public clients).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Task<Result> UpdateClientSecretAsync(string id, string? newClientSecret, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an application (soft delete by default, or hard delete if specified).
    /// </summary>
    /// <param name="id">The application unique identifier.</param>
    /// <param name="hardDelete">If true, performs a hard delete from the database; otherwise, sets status to Deleted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Task<Result> DeleteAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the operational status of an application.
    /// </summary>
    /// <param name="id">The application unique identifier.</param>
    /// <param name="status">The new application status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Task<Result> UpdateStatusAsync(string id, ApplicationStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the operational status of all applications matching the specified environment, or all applications if environment is null.
    /// </summary>
    /// <param name="environment">The optional target environment (null for all environments).</param>
    /// <param name="status">The target status to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the count of updated applications.</returns>
    Task<Result<int>> SetStatusByEnvironmentAsync(ApplicationEnvironment? environment, ApplicationStatus status, CancellationToken cancellationToken = default);
}
