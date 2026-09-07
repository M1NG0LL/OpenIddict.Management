using OpenIddict.Management.Dto;
using OpenIddict.Management.Models;
using OpenIddict.Management.Results;

namespace OpenIddict.Management.Contracts;

/// <summary>
/// Service contract for managing OpenID Connect scope domain models and CRUD operations.
/// </summary>
public interface IScopeManagementService
{
    /// <summary>
    /// Retrieves a scope by its unique identifier.
    /// </summary>
    /// <param name="id">The scope unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the <see cref="ManagedScope"/> if found.</returns>
    Task<Result<ManagedScope>> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a scope by its unique name.
    /// </summary>
    /// <param name="name">The scope name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the <see cref="ManagedScope"/> if found.</returns>
    Task<Result<ManagedScope>> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of scopes.
    /// </summary>
    /// <param name="request">Pagination and search request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a <see cref="PagedResult{T}"/> of <see cref="ManagedScope"/> items.</returns>
    Task<Result<PagedResult<ManagedScope>>> ListAsync(PagedRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new OpenID Connect scope.
    /// </summary>
    /// <param name="name">The unique scope name.</param>
    /// <param name="displayName">The scope display name.</param>
    /// <param name="description">The scope description.</param>
    /// <param name="resources">Optional list of resources associated with the scope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the created <see cref="ManagedScope"/>.</returns>
    Task<Result<ManagedScope>> CreateAsync(
        string name,
        string? displayName,
        string? description,
        IEnumerable<string>? resources = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing OpenID Connect scope.
    /// </summary>
    /// <param name="id">The scope unique identifier.</param>
    /// <param name="displayName">The updated display name.</param>
    /// <param name="description">The updated description.</param>
    /// <param name="resources">Optional updated list of resources.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the updated <see cref="ManagedScope"/>.</returns>
    Task<Result<ManagedScope>> UpdateAsync(
        string id,
        string? displayName,
        string? description,
        IEnumerable<string>? resources = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a scope by its unique identifier.
    /// </summary>
    /// <param name="id">The scope unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
