using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;

namespace OpenIddict.Management.Dashboard.Pages.Tokens;

/// <summary>
/// Page model for Token &amp; Session Inspector, listing, filtering, analytics, and revocation management.
/// </summary>
public class IndexModel(
    IOpenIddictRevocationManager revocationManager,
    ITokenCleanupJobManager? cleanupJobManager = null) : PageModel
{
    /// <summary>Gets or sets the active view tab ("tokens" or "sessions").</summary>
    [BindProperty(SupportsGet = true)]
    public string ActiveTab { get; set; } = "tokens";

    // --- Token Cleanup Background Job Properties ---

    /// <summary>Gets a value indicating whether the cleanup background job is enabled.</summary>
    public bool CleanupJobEnabled { get; set; } = true;

    /// <summary>Gets the cleanup batch size (amount of tokens to clear per run).</summary>
    public int CleanupJobBatchSize { get; set; } = 100;

    /// <summary>Gets the cleanup interval in minutes.</summary>
    public int CleanupJobIntervalMinutes { get; set; } = 60;

    /// <summary>Gets whether revoked tokens are included in cleanup.</summary>
    public bool CleanupJobIncludeRevoked { get; set; } = true;

    /// <summary>Gets the timestamp of the last cleanup run.</summary>
    public DateTimeOffset? CleanupJobLastRun { get; set; }

    /// <summary>Gets the number of tokens pruned on the last cleanup run.</summary>
    public int CleanupJobLastPrunedCount { get; set; }

    /// <summary>Gets the total cumulative tokens pruned.</summary>
    public int CleanupJobTotalPrunedCount { get; set; }

    /// <summary>Gets the current status of the cleanup job.</summary>
    public string CleanupJobStatus { get; set; } = "Idle";

    /// <summary>Gets the last error message, if any.</summary>
    public string? CleanupJobLastError { get; set; }

    // --- Tokens Tab Properties ---

    /// <summary>Gets or sets requested token page index.</summary>
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    /// <summary>Gets or sets token page size.</summary>
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    /// <summary>Gets or sets search term across token ID, reference ID, client ID, or user ID.</summary>
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    /// <summary>Gets or sets user / subject filter.</summary>
    [BindProperty(SupportsGet = true)]
    public string? UserId { get; set; }

    /// <summary>Gets or sets client ID filter.</summary>
    [BindProperty(SupportsGet = true)]
    public string? ClientId { get; set; }

    /// <summary>Gets or sets authorization ID filter.</summary>
    [BindProperty(SupportsGet = true)]
    public string? AuthorizationId { get; set; }

    /// <summary>Gets or sets created from date filter.</summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? CreatedFrom { get; set; }

    /// <summary>Gets or sets created to date filter.</summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? CreatedTo { get; set; }

    /// <summary>Gets or sets token status filter (all, active, revoked).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    /// <summary>Gets or sets token type filter.</summary>
    [BindProperty(SupportsGet = true)]
    public string? TokenType { get; set; }

    /// <summary>Gets or sets token summary counts.</summary>
    public TokenCountSummaryDto TokenCounts { get; set; } = new();

    /// <summary>Gets or sets total valid tokens count.</summary>
    public int ValidCount => TokenCounts.Valid;

    /// <summary>Gets or sets total revoked tokens count.</summary>
    public int RevokedCount => TokenCounts.Revoked;

    /// <summary>Gets or sets total expired tokens count.</summary>
    public int ExpiredCount => TokenCounts.Expired;

    /// <summary>Gets or sets paginated tokens result.</summary>
    public PagedResult<TokenListDto> Tokens { get; set; } = new()
    {
        Items = [],
        PageIndex = 1,
        PageSize = 10,
        TotalCount = 0
    };

    // --- Sessions Tab Properties ---

    /// <summary>Gets or sets requested session page index.</summary>
    [BindProperty(SupportsGet = true)]
    public int SessionPageNumber { get; set; } = 1;

    /// <summary>Gets or sets session page size.</summary>
    [BindProperty(SupportsGet = true)]
    public int SessionPageSize { get; set; } = 10;

    /// <summary>Gets or sets search term for sessions.</summary>
    [BindProperty(SupportsGet = true)]
    public string? SessionSearchTerm { get; set; }

    /// <summary>Gets or sets user / subject filter for sessions.</summary>
    [BindProperty(SupportsGet = true)]
    public string? SessionUserId { get; set; }

    /// <summary>Gets or sets client ID filter for sessions.</summary>
    [BindProperty(SupportsGet = true)]
    public string? SessionClientId { get; set; }

    /// <summary>Gets or sets status filter for sessions (all, active, revoked).</summary>
    [BindProperty(SupportsGet = true)]
    public string? SessionStatus { get; set; }

    /// <summary>Gets or sets paginated sessions result.</summary>
    public PagedResult<SessionListDto> Sessions { get; set; } = new()
    {
        Items = [],
        PageIndex = 1,
        PageSize = 10,
        TotalCount = 0
    };

    // --- Chart Data Properties ---

    /// <summary>Gets or sets aggregated token counts per application for charts.</summary>
    public List<ApplicationTokenCountDto> AppTokenCounts { get; set; } = [];

    /// <summary>Gets or sets daily token timeline data points for charts.</summary>
    public List<TokenTimelineDataPointDto> TokenTimeline { get; set; } = [];

    // --- TempData Notifications ---

    /// <summary>Gets or sets operation result message.</summary>
    [TempData]
    public string? Message { get; set; }

    /// <summary>Gets or sets value indicating whether operation succeeded.</summary>
    [TempData]
    public bool IsSuccess { get; set; }

    /// <summary>Handles GET requests for listing and filtering tokens and sessions.</summary>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        // 1. Load Tokens
        var filter = BuildFilterRequest();
        var result = await revocationManager.ListTokensAsync(filter, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            Tokens = result.Value;
        }

        var countsResult = await revocationManager.GetTokenCountsAsync(cancellationToken);
        if (countsResult.IsSuccess && countsResult.Value is not null)
        {
            TokenCounts = countsResult.Value;
        }

        // 2. Load Sessions
        var sessionFilter = new SessionFilterRequest
        {
            PageIndex = SessionPageNumber,
            PageSize = SessionPageSize,
            SearchTerm = SessionSearchTerm,
            UserId = SessionUserId,
            ClientId = SessionClientId,
            Status = SessionStatus
        };
        var sessionsResult = await revocationManager.ListSessionsAsync(sessionFilter, cancellationToken);
        if (sessionsResult.IsSuccess && sessionsResult.Value is not null)
        {
            Sessions = sessionsResult.Value;
        }

        // 3. Load Chart Data
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = today.AddDays(-29);
        var appCountsResult = await revocationManager.GetTokenCountsByApplicationAsync(cancellationToken);
        if (appCountsResult.IsSuccess && appCountsResult.Value is not null)
        {
            AppTokenCounts = appCountsResult.Value;
        }

        var timelineResult = await revocationManager.GetTokenTimelineAsync(fromDate, today, null, cancellationToken);
        if (timelineResult.IsSuccess && timelineResult.Value is not null)
        {
            TokenTimeline = timelineResult.Value;
        }

        // 4. Load Background Cleanup Job Status
        if (cleanupJobManager is not null)
        {
            CleanupJobEnabled = cleanupJobManager.IsEnabled;
            CleanupJobBatchSize = cleanupJobManager.BatchSize;
            CleanupJobIntervalMinutes = (int)cleanupJobManager.Interval.TotalMinutes;
            CleanupJobIncludeRevoked = cleanupJobManager.IncludeRevoked;
            CleanupJobLastRun = cleanupJobManager.LastRunTime;
            CleanupJobLastPrunedCount = cleanupJobManager.LastPrunedCount;
            CleanupJobTotalPrunedCount = cleanupJobManager.TotalPrunedCount;
            CleanupJobStatus = cleanupJobManager.LastStatus;
            CleanupJobLastError = cleanupJobManager.LastError;
        }
    }

    /// <summary>Handles POST requests for revoking a single token.</summary>
    public async Task<IActionResult> OnPostRevokeSingleAsync(string tokenId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
        {
            Message = "Token ID cannot be empty.";
            IsSuccess = false;
            ActiveTab = "tokens";
            return RedirectToPage(GetRouteValues());
        }

        var result = await revocationManager.RevokeByTokenIdAsync(tokenId, cancellationToken);
        IsSuccess = result.IsSuccess;
        Message = result.IsSuccess
            ? $"Token '{tokenId}' revoked successfully."
            : (result.Error?.Description ?? "Failed to revoke token.");
        ActiveTab = "tokens";

        return RedirectToPage(GetRouteValues());
    }

    /// <summary>Handles POST requests for revoking all tokens matching current filters.</summary>
    public async Task<IActionResult> OnPostRevokeFilteredAsync(CancellationToken cancellationToken)
    {
        var filter = BuildFilterRequest();
        var result = await revocationManager.RevokeTokensWithFilterAsync(filter, cancellationToken);

        IsSuccess = result.IsSuccess;
        if (result.IsSuccess)
        {
            Message = $"Successfully revoked {result.Value?.TokensRevoked ?? 0} token(s) matching the active filters.";
        }
        else
        {
            Message = $"Filtered revocation failed: {result.Error?.Description}";
        }
        ActiveTab = "tokens";

        return RedirectToPage(GetRouteValues());
    }

    /// <summary>Handles POST requests for bulk revoking user tokens.</summary>
    public async Task<IActionResult> OnPostRevokeUserTokensAsync(string targetUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            Message = "User ID cannot be empty.";
            IsSuccess = false;
            ActiveTab = "tokens";
            return RedirectToPage(GetRouteValues());
        }

        var result = await revocationManager.RevokeByUserAsync(targetUserId, cancellationToken);
        IsSuccess = result.IsSuccess;
        Message = result.IsSuccess
            ? $"Tokens for user '{targetUserId}' revoked ({result.Value?.TokensRevoked ?? 0} tokens)."
            : (result.Error?.Description ?? "Failed to revoke tokens.");
        ActiveTab = "tokens";

        return RedirectToPage(GetRouteValues());
    }

    /// <summary>Handles POST requests for bulk revoking client tokens.</summary>
    public async Task<IActionResult> OnPostRevokeClientTokensAsync(string targetClientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetClientId))
        {
            Message = "Client ID cannot be empty.";
            IsSuccess = false;
            ActiveTab = "tokens";
            return RedirectToPage(GetRouteValues());
        }

        var result = await revocationManager.RevokeByClientAsync(targetClientId, cancellationToken);
        IsSuccess = result.IsSuccess;
        Message = result.IsSuccess
            ? $"Tokens for client '{targetClientId}' revoked ({result.Value?.TokensRevoked ?? 0} tokens)."
            : (result.Error?.Description ?? "Failed to revoke tokens.");
        ActiveTab = "tokens";

        return RedirectToPage(GetRouteValues());
    }

    /// <summary>Handles POST requests for revoking a single session authorization.</summary>
    public async Task<IActionResult> OnPostRevokeSessionAsync(string authorizationId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authorizationId))
        {
            Message = "Authorization ID cannot be empty.";
            IsSuccess = false;
            ActiveTab = "sessions";
            return RedirectToPage(GetRouteValues());
        }

        var result = await revocationManager.RevokeSessionAuthorizationsAsync(authorizationId: authorizationId, cancellationToken: cancellationToken);
        IsSuccess = result.IsSuccess;
        Message = result.IsSuccess
            ? $"Session '{authorizationId}' revoked successfully ({result.Value?.TokensRevoked ?? 0} tokens revoked)."
            : (result.Error?.Description ?? "Failed to revoke session authorization.");
        ActiveTab = "sessions";

        return RedirectToPage(GetRouteValues());
    }

    /// <summary>Handles POST requests for revoking all sessions for a user.</summary>
    public async Task<IActionResult> OnPostRevokeUserSessionsAsync(string targetUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            Message = "User ID cannot be empty.";
            IsSuccess = false;
            ActiveTab = "sessions";
            return RedirectToPage(GetRouteValues());
        }

        var result = await revocationManager.RevokeSessionAuthorizationsAsync(userId: targetUserId, cancellationToken: cancellationToken);
        IsSuccess = result.IsSuccess;
        Message = result.IsSuccess
            ? $"All sessions for user '{targetUserId}' revoked ({result.Value?.AuthorizationsRevoked ?? 0} authorizations, {result.Value?.TokensRevoked ?? 0} tokens)."
            : (result.Error?.Description ?? "Failed to revoke user sessions.");
        ActiveTab = "sessions";

        return RedirectToPage(GetRouteValues());
    }

    /// <summary>Handles AJAX requests for fetching timeline data points.</summary>
    public async Task<IActionResult> OnGetTokenTimelineAsync(string? clientId = null, int days = 30, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = today.AddDays(-Math.Max(1, days - 1));
        var result = await revocationManager.GetTokenTimelineAsync(fromDate, today, clientId, cancellationToken);
        return new JsonResult(result.IsSuccess && result.Value is not null ? result.Value : []);
    }

    /// <summary>Handles AJAX requests for fetching per-application token counts.</summary>
    public async Task<IActionResult> OnGetTokensByAppAsync(string? clientId = null, CancellationToken cancellationToken = default)
    {
        var result = await revocationManager.GetTokenCountsByApplicationAsync(cancellationToken);
        var data = result.IsSuccess && result.Value is not null ? result.Value : [];
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            data = data.Where(d => string.Equals(d.ClientId, clientId, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        return new JsonResult(data);
    }

    /// <summary>Handles POST requests to toggle the token cleanup background job enabled/disabled state.</summary>
    public IActionResult OnPostToggleCleanupJob()
    {
        if (cleanupJobManager is null)
        {
            Message = "Cleanup job manager is not available.";
            IsSuccess = false;
            return RedirectToPage(GetRouteValues());
        }

        var newState = !cleanupJobManager.IsEnabled;
        cleanupJobManager.SetEnabled(newState);
        IsSuccess = true;
        Message = newState
            ? "Token cleanup background job has been activated."
            : "Token cleanup background job has been paused.";
        ActiveTab = "tokens";

        return RedirectToPage(GetRouteValues());
    }

    /// <summary>Handles POST requests to update the dynamic amount (batch size) and schedule of the cleanup job.</summary>
    public IActionResult OnPostUpdateCleanupJobSettings(int batchSize, int? intervalMinutes = null, bool? includeRevoked = null)
    {
        if (cleanupJobManager is null)
        {
            Message = "Cleanup job manager is not available.";
            IsSuccess = false;
            return RedirectToPage(GetRouteValues());
        }

        if (batchSize <= 0)
        {
            Message = "Amount (batch size) must be greater than zero.";
            IsSuccess = false;
            ActiveTab = "tokens";
            return RedirectToPage(GetRouteValues());
        }

        TimeSpan? interval = intervalMinutes.HasValue && intervalMinutes.Value > 0
            ? TimeSpan.FromMinutes(intervalMinutes.Value)
            : null;

        cleanupJobManager.UpdateSettings(
            batchSize: batchSize,
            interval: interval,
            includeRevoked: includeRevoked);

        IsSuccess = true;
        Message = $"Cleanup job settings updated: amount set to {batchSize} tokens per cycle.";
        ActiveTab = "tokens";

        return RedirectToPage(GetRouteValues());
    }

    /// <summary>Handles POST requests to trigger an immediate token cleanup pass.</summary>
    public async Task<IActionResult> OnPostRunCleanupJobNowAsync(CancellationToken cancellationToken)
    {
        if (cleanupJobManager is null)
        {
            Message = "Cleanup job manager is not available.";
            IsSuccess = false;
            return RedirectToPage(GetRouteValues());
        }

        var pruned = await cleanupJobManager.TriggerRunAsync(cancellationToken);
        IsSuccess = true;
        Message = $"Manual token cleanup completed: {pruned} expired/revoked token(s) cleared.";
        ActiveTab = "tokens";

        return RedirectToPage(GetRouteValues());
    }

    private TokenFilterRequest BuildFilterRequest() => new()
    {
        PageIndex = PageNumber,
        PageSize = PageSize,
        SearchTerm = SearchTerm,
        UserId = UserId,
        ClientId = ClientId,
        AuthorizationId = AuthorizationId,
        CreatedFrom = CreatedFrom.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(CreatedFrom.Value, DateTimeKind.Utc)) : null,
        CreatedTo = CreatedTo.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(CreatedTo.Value, DateTimeKind.Utc).Date.AddDays(1).AddTicks(-1)) : null,
        Status = Status,
        TokenType = TokenType
    };

    private object GetRouteValues() => new
    {
        activeTab = ActiveTab,
        pageNumber = PageNumber,
        pageSize = PageSize,
        searchTerm = SearchTerm,
        userId = UserId,
        clientId = ClientId,
        authorizationId = AuthorizationId,
        createdFrom = CreatedFrom?.ToString("yyyy-MM-dd"),
        createdTo = CreatedTo?.ToString("yyyy-MM-dd"),
        status = Status,
        tokenType = TokenType,
        sessionPageNumber = SessionPageNumber,
        sessionPageSize = SessionPageSize,
        sessionSearchTerm = SessionSearchTerm,
        sessionUserId = SessionUserId,
        sessionClientId = SessionClientId,
        sessionStatus = SessionStatus
    };
}
