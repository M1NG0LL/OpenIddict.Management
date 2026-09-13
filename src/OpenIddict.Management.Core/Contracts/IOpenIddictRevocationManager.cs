using OpenIddict.Management.Dto;
using OpenIddict.Management.Results;

namespace OpenIddict.Management.Contracts;

/// <summary>
/// Service contract for high-level token and authorization revocation operations.
/// </summary>
public interface IOpenIddictRevocationManager
{
    /// <summary>
    /// Revokes a single token by its unique token identifier.
    /// </summary>
    /// <param name="tokenId">The unique token identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a <see cref="RevocationResultDto"/>.</returns>
    Task<Result<RevocationResultDto>> RevokeByTokenIdAsync(string tokenId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all tokens (access, refresh, authorization codes) associated with the specified user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a <see cref="RevocationResultDto"/>.</returns>
    Task<Result<RevocationResultDto>> RevokeByUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all tokens associated with the specified client application.
    /// </summary>
    /// <param name="clientId">The client application's unique identifier or ClientId.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a <see cref="RevocationResultDto"/>.</returns>
    Task<Result<RevocationResultDto>> RevokeByClientAsync(string clientId, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Prunes expired and inactive tokens from storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the count of pruned tokens.</returns>
    Task<Result<int>> PruneExpiredTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Prunes expired and/or revoked tokens from storage, optionally limited by a maximum batch count.
    /// </summary>
    /// <param name="batchSize">Optional maximum number of tokens to prune in this run. If null or less than or equal to 0, prunes all matching tokens.</param>
    /// <param name="includeRevoked">Whether to also prune revoked tokens in addition to expired tokens. Defaults to true.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the count of pruned tokens.</returns>
    Task<Result<int>> PruneTokensAsync(int? batchSize = null, bool includeRevoked = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of tokens matching the specified filter criteria.
    /// </summary>
    /// <param name="filter">Token filter and pagination criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a <see cref="PagedResult{T}"/> of <see cref="TokenListDto"/>.</returns>
    Task<Result<PagedResult<TokenListDto>>> ListTokensAsync(TokenFilterRequest filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all tokens matching the specified filter criteria.
    /// </summary>
    /// <param name="filter">Token filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a <see cref="RevocationResultDto"/>.</returns>
    Task<Result<RevocationResultDto>> RevokeTokensWithFilterAsync(TokenFilterRequest filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves aggregate counts of tokens categorized by total, valid, expired, and revoked.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a <see cref="TokenCountSummaryDto"/>.</returns>
    Task<Result<TokenCountSummaryDto>> GetTokenCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the count of active authorizations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the count of active authorizations.</returns>
    Task<Result<int>> GetActiveAuthorizationsCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves token counts grouped by application for chart visualizations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="ApplicationTokenCountDto"/>.</returns>
    Task<Result<List<ApplicationTokenCountDto>>> GetTokenCountsByApplicationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves daily token creation counts over a date range, optionally filtered by client identifier.
    /// </summary>
    /// <param name="from">Start date (inclusive).</param>
    /// <param name="to">End date (inclusive).</param>
    /// <param name="clientId">Optional client application identifier to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="TokenTimelineDataPointDto"/>.</returns>
    Task<Result<List<TokenTimelineDataPointDto>>> GetTokenTimelineAsync(
        DateOnly from,
        DateOnly to,
        string? clientId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of authorizations (sessions) matching the specified filter criteria.
    /// </summary>
    /// <param name="filter">Session filter criteria and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a <see cref="PagedResult{T}"/> of <see cref="SessionListDto"/>.</returns>
    Task<Result<PagedResult<SessionListDto>>> ListSessionsAsync(
        SessionFilterRequest filter,
        CancellationToken cancellationToken = default);
}
