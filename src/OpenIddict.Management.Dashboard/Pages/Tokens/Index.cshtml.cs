using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;

namespace OpenIddict.Management.Dashboard.Pages.Tokens;

/// <summary>
/// Page model for Token Inspector, listing, filtering, and revocation management.
/// </summary>
public class IndexModel(IOpenIddictRevocationManager revocationManager) : PageModel
{
    /// <summary>Gets or sets requested page index.</summary>
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    /// <summary>Gets or sets page size.</summary>
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

    /// <summary>Gets or sets operation result message.</summary>
    [TempData]
    public string? Message { get; set; }

    /// <summary>Gets or sets value indicating whether operation succeeded.</summary>
    [TempData]
    public bool IsSuccess { get; set; }

    /// <summary>Handles GET requests for listing and filtering tokens.</summary>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
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
    }

    /// <summary>Handles POST requests for revoking a single token.</summary>
    public async Task<IActionResult> OnPostRevokeSingleAsync(string tokenId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
        {
            Message = "Token ID cannot be empty.";
            IsSuccess = false;
            return RedirectToPage(GetRouteValues());
        }

        var result = await revocationManager.RevokeByTokenIdAsync(tokenId, cancellationToken);
        IsSuccess = result.IsSuccess;
        Message = result.IsSuccess
            ? $"Token '{tokenId}' revoked successfully."
            : (result.Error?.Description ?? "Failed to revoke token.");

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

        return RedirectToPage(GetRouteValues());
    }

    /// <summary>Handles POST requests for bulk revoking user tokens.</summary>
    public async Task<IActionResult> OnPostRevokeUserTokensAsync(string targetUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            Message = "User ID cannot be empty.";
            IsSuccess = false;
            return RedirectToPage(GetRouteValues());
        }

        var result = await revocationManager.RevokeByUserAsync(targetUserId, cancellationToken);
        IsSuccess = result.IsSuccess;
        Message = result.IsSuccess
            ? $"Tokens for user '{targetUserId}' revoked ({result.Value?.TokensRevoked ?? 0} tokens)."
            : (result.Error?.Description ?? "Failed to revoke tokens.");

        return RedirectToPage(GetRouteValues());
    }

    /// <summary>Handles POST requests for bulk revoking client tokens.</summary>
    public async Task<IActionResult> OnPostRevokeClientTokensAsync(string targetClientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetClientId))
        {
            Message = "Client ID cannot be empty.";
            IsSuccess = false;
            return RedirectToPage(GetRouteValues());
        }

        var result = await revocationManager.RevokeByClientAsync(targetClientId, cancellationToken);
        IsSuccess = result.IsSuccess;
        Message = result.IsSuccess
            ? $"Tokens for client '{targetClientId}' revoked ({result.Value?.TokensRevoked ?? 0} tokens)."
            : (result.Error?.Description ?? "Failed to revoke tokens.");

        return RedirectToPage(GetRouteValues());
    }

    private TokenFilterRequest BuildFilterRequest() => new()
    {
        PageIndex = PageNumber,
        PageSize = PageSize,
        SearchTerm = SearchTerm,
        UserId = UserId,
        ClientId = ClientId,
        CreatedFrom = CreatedFrom.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(CreatedFrom.Value, DateTimeKind.Utc)) : null,
        CreatedTo = CreatedTo.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(CreatedTo.Value, DateTimeKind.Utc).Date.AddDays(1).AddTicks(-1)) : null,
        Status = Status,
        TokenType = TokenType
    };

    private object GetRouteValues() => new
    {
        pageNumber = PageNumber,
        pageSize = PageSize,
        searchTerm = SearchTerm,
        userId = UserId,
        clientId = ClientId,
        createdFrom = CreatedFrom?.ToString("yyyy-MM-dd"),
        createdTo = CreatedTo?.ToString("yyyy-MM-dd"),
        status = Status,
        tokenType = TokenType
    };
}
