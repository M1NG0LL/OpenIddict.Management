using OpenIddict.Management.Dto;
using OpenIddict.Management.Results;

namespace OpenIddict.Management.Contracts;

/// <summary>
/// Service contract for managing, querying, extending, revoking, and pruning OpenIddict tokens.
/// Standardized management interface consistent with <see cref="IApplicationManagementService"/> and <see cref="IScopeManagementService"/>.
/// </summary>
public interface ITokenManagementService
{
    /// <summary>
    /// Retrieves a paginated list of tokens matching the specified filter criteria.
    /// </summary>
    /// <param name="filter">Token filter and pagination criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a <see cref="PagedResult{T}"/> of <see cref="TokenListDto"/>.</returns>
    Task<Result<PagedResult<TokenListDto>>> ListTokensAsync(TokenFilterRequest filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves aggregate counts of tokens categorized by total, valid, expired, and revoked.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a <see cref="TokenCountSummaryDto"/>.</returns>
    Task<Result<TokenCountSummaryDto>> GetTokenCountsAsync(CancellationToken cancellationToken = default);

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
    /// Extends the expiration date of one or more tokens (both valid and expired) by a given number of minutes.
    /// </summary>
    /// <param name="tokenIds">The collection of token identifiers whose expiration will be extended.</param>
    /// <param name="additionalMinutes">The additional minutes to add to the expiration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the count of tokens successfully updated.</returns>
    Task<Result<int>> ExtendTokenExpirationAsync(IReadOnlyList<string> tokenIds, int additionalMinutes, CancellationToken cancellationToken = default);

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
    /// Revokes multiple tokens by their unique identifiers.
    /// </summary>
    /// <param name="tokenIds">The collection of token identifiers to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a <see cref="RevocationResultDto"/>.</returns>
    Task<Result<RevocationResultDto>> RevokeMultipleTokensAsync(IReadOnlyList<string> tokenIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all tokens matching the specified filter criteria.
    /// </summary>
    /// <param name="filter">Token filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a <see cref="RevocationResultDto"/>.</returns>
    Task<Result<RevocationResultDto>> RevokeTokensWithFilterAsync(TokenFilterRequest filter, CancellationToken cancellationToken = default);

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
    /// Decodes and introspects a token by its database identifier or reference identifier for debugging and inspection.
    /// </summary>
    /// <param name="idOrReferenceId">The token database identifier or reference identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the <see cref="TokenIntrospectionDto"/>.</returns>
    Task<Result<TokenIntrospectionDto>> IntrospectTokenAsync(string idOrReferenceId, CancellationToken cancellationToken = default);
}
