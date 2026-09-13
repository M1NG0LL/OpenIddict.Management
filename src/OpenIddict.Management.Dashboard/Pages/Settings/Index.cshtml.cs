using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Options;

namespace OpenIddict.Management.Dashboard.Pages.Settings;

/// <summary>
/// Page model for system and dashboard settings, including background jobs and token pruning policies.
/// </summary>
public class IndexModel(
    ITokenCleanupJobManager? cleanupJobManager = null,
    IOptions<DashboardOptions>? dashboardOptions = null) : PageModel
{
    /// <summary>Gets or sets the active settings tab ("tokens", "general").</summary>
    [BindProperty(SupportsGet = true)]
    public string ActiveTab { get; set; } = "general";

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

    // --- General Dashboard Settings Properties ---

    /// <summary>Gets the dashboard title.</summary>
    public string DashboardTitle => dashboardOptions?.Value.DashboardTitle ?? "OpenIddict Management";

    /// <summary>Gets the dashboard path prefix.</summary>
    public string PathPrefix => dashboardOptions?.Value.PathPrefix ?? "/management";

    /// <summary>Gets the dashboard exit URL.</summary>
    public string? ExitUrl => dashboardOptions?.Value.ExitUrl;

    /// <summary>Gets whether authorization is required.</summary>
    public bool RequireAuthorization => dashboardOptions?.Value.RequireAuthorization ?? true;

    /// <summary>Gets the authorization policy name.</summary>
    public string? AuthorizationPolicy => dashboardOptions?.Value.AuthorizationPolicy;

    // --- TempData Notifications ---

    /// <summary>Gets or sets operation result message.</summary>
    [TempData]
    public string? Message { get; set; }

    /// <summary>Gets or sets value indicating whether operation succeeded.</summary>
    [TempData]
    public bool IsSuccess { get; set; }

    /// <summary>Handles GET requests for settings page.</summary>
    public void OnGet()
    {
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

    /// <summary>Handles POST requests to toggle the token cleanup background job enabled/disabled state.</summary>
    public IActionResult OnPostToggleCleanupJob()
    {
        if (cleanupJobManager is null)
        {
            Message = "Cleanup job manager is not available.";
            IsSuccess = false;
            return RedirectToPage(new { activeTab = "tokens" });
        }

        var newState = !cleanupJobManager.IsEnabled;
        cleanupJobManager.SetEnabled(newState);
        IsSuccess = true;
        Message = newState
            ? "Token cleanup background job has been activated."
            : "Token cleanup background job has been paused.";

        return RedirectToPage(new { activeTab = "tokens" });
    }

    /// <summary>Handles POST requests to update the dynamic amount (batch size) and schedule of the cleanup job.</summary>
    public IActionResult OnPostUpdateCleanupJobSettings(int batchSize, int? intervalMinutes = null, bool? includeRevoked = null)
    {
        if (cleanupJobManager is null)
        {
            Message = "Cleanup job manager is not available.";
            IsSuccess = false;
            return RedirectToPage(new { activeTab = "tokens" });
        }

        if (batchSize <= 0)
        {
            Message = "Amount (batch size) must be greater than zero.";
            IsSuccess = false;
            return RedirectToPage(new { activeTab = "tokens" });
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

        return RedirectToPage(new { activeTab = "tokens" });
    }

    /// <summary>Handles POST requests to trigger an immediate token cleanup pass.</summary>
    public async Task<IActionResult> OnPostRunCleanupJobNowAsync(CancellationToken cancellationToken)
    {
        if (cleanupJobManager is null)
        {
            Message = "Cleanup job manager is not available.";
            IsSuccess = false;
            return RedirectToPage(new { activeTab = "tokens" });
        }

        var pruned = await cleanupJobManager.TriggerRunAsync(cancellationToken);
        IsSuccess = true;
        Message = $"Manual token cleanup completed: {pruned} expired/revoked token(s) cleared.";

        return RedirectToPage(new { activeTab = "tokens" });
    }
}
