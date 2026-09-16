using OpenIddict.Management.Dto;
using OpenIddict.Management.Results;

namespace OpenIddict.Management.Contracts;

/// <summary>
/// Service contract for managing, querying, and revoking OpenIddict session authorizations.
/// </summary>
public interface IOpenIddictAuthorizationManager
{
    /// <summary>
    /// Retrieves a paginated list of authorizations (sessions) matching the specified filter criteria.
    /// </summary>
    /// <param name="filter">Session filter criteria and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a <see cref="PagedResult{T}"/> of <see cref="SessionListDto"/>.</returns>
    Task<Result<PagedResult<SessionListDto>>> ListSessionsAsync(
        SessionFilterRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the count of active authorizations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the count of active authorizations.</returns>
    Task<Result<int>> GetActiveAuthorizationsCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes session authorizations for a specified user or authorization ID.
    /// </summary>
    /// <param name="userId">Optional user ID.</param>
    /// <param name="authorizationId">Optional authorization ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a <see cref="RevocationResultDto"/>.</returns>
    Task<Result<RevocationResultDto>> RevokeSessionAuthorizationsAsync(
        string? userId = null,
        string? authorizationId = null,
        CancellationToken cancellationToken = default);
}
