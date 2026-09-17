using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Models;
using OpenIddict.Management.Options;
using OpenIddict.Server;

namespace OpenIddict.Management.Dashboard.Pages.Settings;

/// <summary>
/// Page model for system and dashboard settings, including OpenIddict runtime server configuration, background jobs, and token pruning policies.
/// </summary>
public class IndexModel(
    ITokenCleanupJobManager? cleanupJobManager = null,
    IOptions<DashboardOptions>? dashboardOptions = null,
    IOptionsMonitor<OpenIddictServerOptions>? serverOptionsMonitor = null,
    IOptions<OpenIddictServerOptions>? serverOptions = null,
    IScopeManagementService? scopeService = null,
    IConfigurationExportImportService? exportImportService = null,
    IApplicationManagementService? applicationService = null) : PageModel
{
    /// <summary>Gets or sets the active settings tab ("general", "oidc", "tokens", "export-import").</summary>
    [BindProperty(SupportsGet = true)]
    public string ActiveTab { get; set; } = "general";

    /// <summary>Gets whether the configuration export/import service is available.</summary>
    public bool IsExportImportAvailable => exportImportService is not null;

    /// <summary>Gets the total registered applications count.</summary>
    public int TotalApplicationsCount { get; set; }

    /// <summary>Gets the total registered scopes count.</summary>
    public int TotalScopesCount { get; set; }

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

    // --- OpenIddict Server Configuration Properties ---

    /// <summary>Gets whether OpenIddict Server options are available in DI.</summary>
    public bool IsServerConfigured => GetServerOptions() is not null;

    /// <summary>Gets the configured server issuer URI.</summary>
    public string? OidcIssuer { get; set; }

    /// <summary>Gets the full resolved URL for the OpenID Connect discovery document (.well-known/openid-configuration).</summary>
    public string? DiscoveryEndpointUrl { get; set; }

    /// <summary>Gets the full resolved URL for the JSON Web Key Set (.well-known/jwks).</summary>
    public string? JwksEndpointUrl { get; set; }

    // Static Endpoints (Read-Only)
    /// <summary>Gets authorization endpoint URIs.</summary>
    public IReadOnlyList<string> AuthorizationEndpointUris { get; set; } = [];

    /// <summary>Gets token endpoint URIs.</summary>
    public IReadOnlyList<string> TokenEndpointUris { get; set; } = [];

    /// <summary>Gets logout endpoint URIs.</summary>
    public IReadOnlyList<string> LogoutEndpointUris { get; set; } = [];

    /// <summary>Gets userinfo endpoint URIs.</summary>
    public IReadOnlyList<string> UserinfoEndpointUris { get; set; } = [];

    /// <summary>Gets introspection endpoint URIs.</summary>
    public IReadOnlyList<string> IntrospectionEndpointUris { get; set; } = [];

    /// <summary>Gets revocation endpoint URIs.</summary>
    public IReadOnlyList<string> RevocationEndpointUris { get; set; } = [];

    /// <summary>Gets device endpoint URIs.</summary>
    public IReadOnlyList<string> DeviceEndpointUris { get; set; } = [];

    /// <summary>Gets verification endpoint URIs.</summary>
    public IReadOnlyList<string> VerificationEndpointUris { get; set; } = [];

    /// <summary>Gets cryptography endpoint URIs.</summary>
    public IReadOnlyList<string> CryptographyEndpointUris { get; set; } = [];

    /// <summary>Gets configuration discovery endpoint URIs.</summary>
    public IReadOnlyList<string> ConfigurationEndpointUris { get; set; } = [];

    // Grant Types & Flows
    /// <summary>Gets or sets whether Authorization Code flow is enabled.</summary>
    public bool AllowAuthorizationCodeFlow { get; set; }

    /// <summary>Gets or sets whether Client Credentials flow is enabled.</summary>
    public bool AllowClientCredentialsFlow { get; set; }

    /// <summary>Gets or sets whether Refresh Token flow is enabled.</summary>
    public bool AllowRefreshTokenFlow { get; set; }

    /// <summary>Gets or sets whether Resource Owner Password flow is enabled.</summary>
    public bool AllowPasswordFlow { get; set; }

    /// <summary>Gets or sets whether Implicit flow is enabled.</summary>
    public bool AllowImplicitFlow { get; set; }

    /// <summary>Gets or sets whether Device Code flow is enabled.</summary>
    public bool AllowDeviceCodeFlow { get; set; }

    /// <summary>Gets or sets custom grant types (comma-separated).</summary>
    public string? CustomGrantTypes { get; set; }

    // Token Lifetimes
    /// <summary>Gets or sets access token lifetime in minutes.</summary>
    public double? AccessTokenLifetimeMinutes { get; set; }

    /// <summary>Gets or sets refresh token lifetime in days.</summary>
    public double? RefreshTokenLifetimeDays { get; set; }

    /// <summary>Gets or sets authorization code lifetime in minutes.</summary>
    public double? AuthorizationCodeLifetimeMinutes { get; set; }

    /// <summary>Gets or sets identity token lifetime in minutes.</summary>
    public double? IdentityTokenLifetimeMinutes { get; set; }

    /// <summary>Gets or sets device code lifetime in minutes.</summary>
    public double? DeviceCodeLifetimeMinutes { get; set; }

    /// <summary>Gets or sets user code lifetime in minutes.</summary>
    public double? UserCodeLifetimeMinutes { get; set; }

    /// <summary>Gets or sets refresh token reuse leeway in seconds.</summary>
    public double? RefreshTokenReuseLeewaySeconds { get; set; }

    /// <summary>Gets formatted access token lifetime description.</summary>
    public string AccessTokenLifetimeDescription =>
        AccessTokenLifetimeMinutes.HasValue
            ? $"{(AccessTokenLifetimeMinutes.Value >= 1440 ? (AccessTokenLifetimeMinutes.Value / 1440).ToString("0.#") + " days" : (AccessTokenLifetimeMinutes.Value >= 60 ? (AccessTokenLifetimeMinutes.Value / 60).ToString("0.#") + " hours" : AccessTokenLifetimeMinutes.Value + " mins"))}"
            : "Default (1 hour)";

    /// <summary>Gets formatted refresh token lifetime description.</summary>
    public string RefreshTokenLifetimeDescription =>
        RefreshTokenLifetimeDays.HasValue
            ? $"{RefreshTokenLifetimeDays.Value:0.#} days"
            : "Default (14 days)";

    /// <summary>Gets formatted authorization code lifetime description.</summary>
    public string AuthorizationCodeLifetimeDescription =>
        AuthorizationCodeLifetimeMinutes.HasValue
            ? $"{AuthorizationCodeLifetimeMinutes.Value:0.#} mins"
            : "Default (5 mins)";

    /// <summary>Gets formatted identity token lifetime description.</summary>
    public string IdentityTokenLifetimeDescription =>
        IdentityTokenLifetimeMinutes.HasValue
            ? $"{IdentityTokenLifetimeMinutes.Value:0.#} mins"
            : "Default (20 mins)";

    /// <summary>Gets formatted device code lifetime description.</summary>
    public string DeviceCodeLifetimeDescription =>
        DeviceCodeLifetimeMinutes.HasValue
            ? $"{DeviceCodeLifetimeMinutes.Value:0.#} mins"
            : "Default (10 mins)";

    /// <summary>Gets formatted refresh token reuse leeway description.</summary>
    public string RefreshTokenReuseLeewayDescription =>
        RefreshTokenReuseLeewaySeconds.HasValue
            ? $"{RefreshTokenReuseLeewaySeconds.Value:0.#}s grace"
            : "None (Strict)";

    // Registered Scopes
    /// <summary>Gets or sets selected registered scopes.</summary>
    public List<string> SelectedRegisteredScopes { get; set; } = [];

    /// <summary>Gets or sets custom scopes text.</summary>
    public string? CustomScopesText { get; set; }

    /// <summary>Gets available database scopes.</summary>
    public IReadOnlyList<ManagedScope> DatabaseScopes { get; set; } = [];

    // Security & Storage Policies
    /// <summary>Gets or sets whether PKCE is required.</summary>
    public bool RequireProofKeyForCodeExchange { get; set; }

    /// <summary>Gets or sets whether access token encryption is disabled.</summary>
    public bool DisableAccessTokenEncryption { get; set; }

    /// <summary>Gets or sets whether rolling refresh tokens are disabled.</summary>
    public bool DisableRollingRefreshTokens { get; set; }

    /// <summary>Gets or sets whether sliding refresh token expiration is disabled.</summary>
    public bool DisableSlidingRefreshTokenExpiration { get; set; }

    /// <summary>Gets or sets whether reference access tokens are used.</summary>
    public bool UseReferenceAccessTokens { get; set; }

    /// <summary>Gets or sets whether reference refresh tokens are used.</summary>
    public bool UseReferenceRefreshTokens { get; set; }

    /// <summary>Gets or sets whether token storage is disabled.</summary>
    public bool DisableTokenStorage { get; set; }

    /// <summary>Gets or sets whether authorization storage is disabled.</summary>
    public bool DisableAuthorizationStorage { get; set; }

    /// <summary>Gets or sets whether scope validation is disabled.</summary>
    public bool DisableScopeValidation { get; set; }

    /// <summary>Gets or sets whether anonymous clients are accepted.</summary>
    public bool AcceptAnonymousClients { get; set; }

    /// <summary>Gets or sets whether endpoint permissions are ignored.</summary>
    public bool IgnoreEndpointPermissions { get; set; }

    /// <summary>Gets or sets whether grant type permissions are ignored.</summary>
    public bool IgnoreGrantTypePermissions { get; set; }

    /// <summary>Gets or sets whether response type permissions are ignored.</summary>
    public bool IgnoreResponseTypePermissions { get; set; }

    /// <summary>Gets or sets whether scope permissions are ignored.</summary>
    public bool IgnoreScopePermissions { get; set; }

    /// <summary>Gets or sets whether degraded mode is enabled.</summary>
    public bool EnableDegradedMode { get; set; }

    // Device Flow & Response Modes
    /// <summary>Gets or sets user code length.</summary>
    public int? UserCodeLength { get; set; }

    /// <summary>Gets or sets user code display format.</summary>
    public string? UserCodeDisplayFormat { get; set; }

    /// <summary>Gets or sets selected response types.</summary>
    public List<string> SelectedResponseTypes { get; set; } = [];

    /// <summary>Gets or sets selected response modes.</summary>
    public List<string> SelectedResponseModes { get; set; } = [];

    // --- TempData Notifications ---

    /// <summary>Gets or sets operation result message.</summary>
    [TempData]
    public string? Message { get; set; }

    /// <summary>Gets or sets value indicating whether operation succeeded.</summary>
    [TempData]
    public bool IsSuccess { get; set; }

    /// <summary>Gets the active OpenIddictServerOptions instance, if registered.</summary>
    public OpenIddictServerOptions? GetServerOptions() =>
        serverOptionsMonitor?.CurrentValue ?? serverOptions?.Value;

    /// <summary>Handles GET requests asynchronously for settings page.</summary>
    public async Task OnGetAsync(CancellationToken cancellationToken = default)
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

        var opt = GetServerOptions();
        if (opt is not null)
        {
            OidcIssuer = opt.Issuer?.ToString();

            // Endpoints
            AuthorizationEndpointUris = opt.AuthorizationEndpointUris.Select(u => u.OriginalString).ToList();
            TokenEndpointUris = opt.TokenEndpointUris.Select(u => u.OriginalString).ToList();
            LogoutEndpointUris = opt.LogoutEndpointUris.Select(u => u.OriginalString).ToList();
            UserinfoEndpointUris = opt.UserinfoEndpointUris.Select(u => u.OriginalString).ToList();
            IntrospectionEndpointUris = opt.IntrospectionEndpointUris.Select(u => u.OriginalString).ToList();
            RevocationEndpointUris = opt.RevocationEndpointUris.Select(u => u.OriginalString).ToList();
            DeviceEndpointUris = opt.DeviceEndpointUris.Select(u => u.OriginalString).ToList();
            VerificationEndpointUris = opt.VerificationEndpointUris.Select(u => u.OriginalString).ToList();
            CryptographyEndpointUris = opt.CryptographyEndpointUris.Select(u => u.OriginalString).ToList();
            ConfigurationEndpointUris = opt.ConfigurationEndpointUris.Select(u => u.OriginalString).ToList();

            var discoveryRelUri = ConfigurationEndpointUris.FirstOrDefault() ?? "/.well-known/openid-configuration";
            DiscoveryEndpointUrl = ResolveEndpointUrl(discoveryRelUri);

            var jwksRelUri = CryptographyEndpointUris.FirstOrDefault() ?? "/.well-known/jwks";
            JwksEndpointUrl = ResolveEndpointUrl(jwksRelUri);

            // Flows
            AllowAuthorizationCodeFlow = opt.GrantTypes.Contains(OpenIddictConstants.GrantTypes.AuthorizationCode);
            AllowClientCredentialsFlow = opt.GrantTypes.Contains(OpenIddictConstants.GrantTypes.ClientCredentials);
            AllowRefreshTokenFlow = opt.GrantTypes.Contains(OpenIddictConstants.GrantTypes.RefreshToken);
            AllowPasswordFlow = opt.GrantTypes.Contains(OpenIddictConstants.GrantTypes.Password);
            AllowImplicitFlow = opt.GrantTypes.Contains(OpenIddictConstants.GrantTypes.Implicit);
            AllowDeviceCodeFlow = opt.GrantTypes.Contains(OpenIddictConstants.GrantTypes.DeviceCode);

            var standardGrants = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                OpenIddictConstants.GrantTypes.AuthorizationCode,
                OpenIddictConstants.GrantTypes.ClientCredentials,
                OpenIddictConstants.GrantTypes.RefreshToken,
                OpenIddictConstants.GrantTypes.Password,
                OpenIddictConstants.GrantTypes.Implicit,
                OpenIddictConstants.GrantTypes.DeviceCode
            };
            var customs = opt.GrantTypes.Where(g => !standardGrants.Contains(g)).ToList();
            CustomGrantTypes = customs.Count > 0 ? string.Join(", ", customs) : null;

            // Lifetimes
            AccessTokenLifetimeMinutes = opt.AccessTokenLifetime?.TotalMinutes;
            RefreshTokenLifetimeDays = opt.RefreshTokenLifetime?.TotalDays;
            AuthorizationCodeLifetimeMinutes = opt.AuthorizationCodeLifetime?.TotalMinutes;
            IdentityTokenLifetimeMinutes = opt.IdentityTokenLifetime?.TotalMinutes;
            DeviceCodeLifetimeMinutes = opt.DeviceCodeLifetime?.TotalMinutes;
            UserCodeLifetimeMinutes = opt.UserCodeLifetime?.TotalMinutes;
            RefreshTokenReuseLeewaySeconds = opt.RefreshTokenReuseLeeway?.TotalSeconds;

            // Scopes
            SelectedRegisteredScopes = opt.Scopes.ToList();

            // Security & Storage
            RequireProofKeyForCodeExchange = opt.RequireProofKeyForCodeExchange;
            DisableAccessTokenEncryption = opt.DisableAccessTokenEncryption;
            DisableRollingRefreshTokens = opt.DisableRollingRefreshTokens;
            DisableSlidingRefreshTokenExpiration = opt.DisableSlidingRefreshTokenExpiration;
            UseReferenceAccessTokens = opt.UseReferenceAccessTokens;
            UseReferenceRefreshTokens = opt.UseReferenceRefreshTokens;
            DisableTokenStorage = opt.DisableTokenStorage;
            DisableAuthorizationStorage = opt.DisableAuthorizationStorage;
            DisableScopeValidation = opt.DisableScopeValidation;
            AcceptAnonymousClients = opt.AcceptAnonymousClients;
            IgnoreEndpointPermissions = opt.IgnoreEndpointPermissions;
            IgnoreGrantTypePermissions = opt.IgnoreGrantTypePermissions;
            IgnoreResponseTypePermissions = opt.IgnoreResponseTypePermissions;
            IgnoreScopePermissions = opt.IgnoreScopePermissions;
            EnableDegradedMode = opt.EnableDegradedMode;

            // Device & Response
            UserCodeLength = opt.UserCodeLength;
            UserCodeDisplayFormat = opt.UserCodeDisplayFormat;
            SelectedResponseTypes = opt.ResponseTypes.ToList();
            SelectedResponseModes = opt.ResponseModes.ToList();
        }

        if (applicationService is not null)
        {
            var appResult = await applicationService.ListAsync(new PagedRequest { PageIndex = 1, PageSize = 1 }, cancellationToken: cancellationToken);
            if (appResult.IsSuccess && appResult.Value is not null)
            {
                TotalApplicationsCount = appResult.Value.TotalCount;
            }
        }

        if (scopeService is not null)
        {
            var scopeResult = await scopeService.ListAsync(new PagedRequest { PageIndex = 1, PageSize = 1000 }, cancellationToken);
            if (scopeResult.IsSuccess && scopeResult.Value is not null)
            {
                DatabaseScopes = scopeResult.Value.Items;
                TotalScopesCount = scopeResult.Value.TotalCount;
            }
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

    /// <summary>Handles POST requests to update OpenIddict server configuration.</summary>
    public IActionResult OnPostUpdateOidcSettings(
        bool allowAuthCode = false,
        bool allowClientCreds = false,
        bool allowRefreshToken = false,
        bool allowPassword = false,
        bool allowImplicit = false,
        bool allowDeviceCode = false,
        string? customGrantTypes = null,
        double? accessTokenMinutes = null,
        double? refreshTokenDays = null,
        double? authCodeMinutes = null,
        double? identityTokenMinutes = null,
        double? deviceCodeMinutes = null,
        double? userCodeMinutes = null,
        double? reuseLeewaySeconds = null,
        List<string>? selectedScopes = null,
        string? customScopesText = null,
        bool requirePkce = false,
        bool disableTokenEncryption = false,
        bool disableRollingRefresh = false,
        bool disableSlidingRefresh = false,
        bool useRefAccessTokens = false,
        bool useRefRefreshTokens = false,
        bool disableTokenStorage = false,
        bool disableAuthStorage = false,
        bool disableScopeValidation = false,
        bool acceptAnonymousClients = false,
        bool ignoreEndpointPermissions = false,
        bool ignoreGrantTypePermissions = false,
        bool ignoreResponseTypePermissions = false,
        bool ignoreScopePermissions = false,
        bool enableDegradedMode = false,
        int? userCodeLength = null,
        string? userCodeDisplayFormat = null,
        List<string>? selectedResponseTypes = null,
        List<string>? selectedResponseModes = null)
    {
        var options = GetServerOptions();
        if (options is null)
        {
            Message = "OpenIddict server configuration is not registered in this application.";
            IsSuccess = false;
            return RedirectToPage(new { activeTab = "oidc" });
        }

        // 1. Update Grant Types / Flows
        options.GrantTypes.Clear();
        if (allowAuthCode) options.GrantTypes.Add(OpenIddictConstants.GrantTypes.AuthorizationCode);
        if (allowClientCreds) options.GrantTypes.Add(OpenIddictConstants.GrantTypes.ClientCredentials);
        if (allowRefreshToken) options.GrantTypes.Add(OpenIddictConstants.GrantTypes.RefreshToken);
        if (allowPassword) options.GrantTypes.Add(OpenIddictConstants.GrantTypes.Password);
        if (allowImplicit) options.GrantTypes.Add(OpenIddictConstants.GrantTypes.Implicit);
        if (allowDeviceCode) options.GrantTypes.Add(OpenIddictConstants.GrantTypes.DeviceCode);

        if (!string.IsNullOrWhiteSpace(customGrantTypes))
        {
            var customs = customGrantTypes.Split([',', ';', ' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var cg in customs)
            {
                options.GrantTypes.Add(cg);
            }
        }

        // 2. Update Registered Scopes
        options.Scopes.Clear();
        if (selectedScopes is not null)
        {
            foreach (var sc in selectedScopes)
            {
                if (!string.IsNullOrWhiteSpace(sc))
                {
                    options.Scopes.Add(sc.Trim());
                }
            }
        }
        if (!string.IsNullOrWhiteSpace(customScopesText))
        {
            var customs = customScopesText.Split([',', ';', ' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var cs in customs)
            {
                options.Scopes.Add(cs);
            }
        }

        // 3. Update Lifetimes (preserve existing if flow is disabled and value omitted)
        if (accessTokenMinutes.HasValue)
        {
            options.AccessTokenLifetime = accessTokenMinutes.Value > 0
                ? TimeSpan.FromMinutes(accessTokenMinutes.Value)
                : null;
        }

        if (allowRefreshToken || refreshTokenDays.HasValue)
        {
            options.RefreshTokenLifetime = refreshTokenDays is > 0
                ? TimeSpan.FromDays(refreshTokenDays.Value)
                : null;
        }

        if (allowAuthCode || authCodeMinutes.HasValue)
        {
            options.AuthorizationCodeLifetime = authCodeMinutes is > 0
                ? TimeSpan.FromMinutes(authCodeMinutes.Value)
                : null;
        }

        if (allowAuthCode || allowImplicit || identityTokenMinutes.HasValue)
        {
            options.IdentityTokenLifetime = identityTokenMinutes is > 0
                ? TimeSpan.FromMinutes(identityTokenMinutes.Value)
                : null;
        }

        if (allowDeviceCode || deviceCodeMinutes.HasValue)
        {
            options.DeviceCodeLifetime = deviceCodeMinutes is > 0
                ? TimeSpan.FromMinutes(deviceCodeMinutes.Value)
                : null;
        }

        if (userCodeMinutes.HasValue)
        {
            options.UserCodeLifetime = userCodeMinutes.Value > 0
                ? TimeSpan.FromMinutes(userCodeMinutes.Value)
                : null;
        }

        if (allowRefreshToken || reuseLeewaySeconds.HasValue)
        {
            options.RefreshTokenReuseLeeway = reuseLeewaySeconds is >= 0
                ? TimeSpan.FromSeconds(reuseLeewaySeconds.Value)
                : null;
        }

        // 4. Update Security & Storage
        if (allowAuthCode || requirePkce)
        {
            options.RequireProofKeyForCodeExchange = requirePkce;
        }

        options.DisableAccessTokenEncryption = disableTokenEncryption;

        if (allowRefreshToken || disableRollingRefresh)
        {
            options.DisableRollingRefreshTokens = disableRollingRefresh;
        }

        if (allowRefreshToken || disableSlidingRefresh)
        {
            options.DisableSlidingRefreshTokenExpiration = disableSlidingRefresh;
        }

        options.UseReferenceAccessTokens = useRefAccessTokens;

        if (allowRefreshToken || useRefRefreshTokens)
        {
            options.UseReferenceRefreshTokens = useRefRefreshTokens;
        }

        options.DisableTokenStorage = disableTokenStorage;
        options.DisableAuthorizationStorage = disableAuthStorage;
        options.DisableScopeValidation = disableScopeValidation;
        options.AcceptAnonymousClients = acceptAnonymousClients;
        options.IgnoreEndpointPermissions = ignoreEndpointPermissions;
        options.IgnoreGrantTypePermissions = ignoreGrantTypePermissions;
        options.IgnoreResponseTypePermissions = ignoreResponseTypePermissions;
        options.IgnoreScopePermissions = ignoreScopePermissions;
        options.EnableDegradedMode = enableDegradedMode;

        // 5. Update Response Types & Modes if supplied
        if (selectedResponseTypes is not null && selectedResponseTypes.Count > 0)
        {
            options.ResponseTypes.Clear();
            foreach (var rt in selectedResponseTypes)
            {
                if (!string.IsNullOrWhiteSpace(rt)) options.ResponseTypes.Add(rt.Trim());
            }
        }
        else
        {
            // Maintain consistency with standard flows
            if (allowAuthCode && !options.ResponseTypes.Contains(OpenIddictConstants.ResponseTypes.Code))
            {
                options.ResponseTypes.Add(OpenIddictConstants.ResponseTypes.Code);
            }
            if (allowImplicit && !options.ResponseTypes.Contains(OpenIddictConstants.ResponseTypes.Token))
            {
                options.ResponseTypes.Add(OpenIddictConstants.ResponseTypes.Token);
            }
        }

        if (selectedResponseModes is not null && selectedResponseModes.Count > 0)
        {
            options.ResponseModes.Clear();
            foreach (var rm in selectedResponseModes)
            {
                if (!string.IsNullOrWhiteSpace(rm)) options.ResponseModes.Add(rm.Trim());
            }
        }

        // 6. Code challenge methods for PKCE
        if (requirePkce)
        {
            options.CodeChallengeMethods.Add(OpenIddictConstants.CodeChallengeMethods.Sha256);
            options.CodeChallengeMethods.Add(OpenIddictConstants.CodeChallengeMethods.Plain);
        }

        // 7. Device flow settings
        if (userCodeLength is > 0)
        {
            options.UserCodeLength = userCodeLength.Value;
        }
        if (!string.IsNullOrWhiteSpace(userCodeDisplayFormat))
        {
            options.UserCodeDisplayFormat = userCodeDisplayFormat.Trim();
        }

        IsSuccess = true;
        Message = "OpenIddict server configuration updated successfully. Changes are active immediately.";

        return RedirectToPage(new { activeTab = "oidc" });
    }

    /// <summary>Handles POST requests to export the system configuration package as a downloadable JSON file.</summary>
    public async Task<IActionResult> OnPostExportJsonAsync(CancellationToken cancellationToken = default)
    {
        if (exportImportService is null)
        {
            Message = "Configuration export/import service is not registered in the application.";
            IsSuccess = false;
            return RedirectToPage(new { activeTab = "export-import" });
        }

        var result = await exportImportService.ExportConfigurationAsJsonAsync(cancellationToken);
        if (result.IsFailure)
        {
            Message = $"Export failed: {result.Error?.Description ?? "Unknown error"}";
            IsSuccess = false;
            return RedirectToPage(new { activeTab = "export-import" });
        }

        var fileName = $"openiddict-configuration-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json";
        var bytes = System.Text.Encoding.UTF8.GetBytes(result.Value!);
        return File(bytes, "application/json", fileName);
    }

    /// <summary>Handles GET requests to download the exported JSON file directly via a link.</summary>
    public async Task<IActionResult> OnGetExportJsonAsync(CancellationToken cancellationToken = default)
    {
        return await OnPostExportJsonAsync(cancellationToken);
    }

    /// <summary>Handles POST requests to import a configuration package from an uploaded JSON file or raw JSON text.</summary>
    public async Task<IActionResult> OnPostImportJsonAsync(
        IFormFile? importFile,
        string? importJsonText,
        bool overwriteExisting = false,
        bool importApplications = true,
        bool importScopes = true,
        bool importConfigurations = true,
        CancellationToken cancellationToken = default)
    {
        if (exportImportService is null)
        {
            Message = "Configuration export/import service is not registered in the application.";
            IsSuccess = false;
            return RedirectToPage(new { activeTab = "export-import" });
        }

        string json;
        if (importFile is not null && importFile.Length > 0)
        {
            using var reader = new StreamReader(importFile.OpenReadStream());
            json = await reader.ReadToEndAsync(cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(importJsonText))
        {
            json = importJsonText;
        }
        else
        {
            Message = "Please select a JSON file to upload or paste valid configuration JSON text.";
            IsSuccess = false;
            return RedirectToPage(new { activeTab = "export-import" });
        }

        var options = new ImportOptions
        {
            OverwriteExisting = overwriteExisting,
            ImportApplications = importApplications,
            ImportScopes = importScopes,
            ImportConfigurations = importConfigurations
        };

        var result = await exportImportService.ImportConfigurationFromJsonAsync(json, options, cancellationToken);
        if (result.IsFailure)
        {
            Message = $"Import failed: {result.Error?.Description ?? "Invalid export package"}";
            IsSuccess = false;
            return RedirectToPage(new { activeTab = "export-import" });
        }

        var res = result.Value!;
        var summary = $"Import completed. Apps: +{res.ApplicationsCreated} updated:{res.ApplicationsUpdated} skipped:{res.ApplicationsSkipped} | Scopes: +{res.ScopesCreated} updated:{res.ScopesUpdated} skipped:{res.ScopesSkipped}";
        if (options.ImportConfigurations)
        {
            summary += $" | Server Config: {(res.OpenIddictServerConfigImported ? "Applied" : "Skipped")} | Mgmt Config: {(res.ManagementConfigImported ? "Applied" : "Skipped")}";
        }
        if (res.Errors.Count > 0)
        {
            summary += $" (Warnings: {string.Join("; ", res.Errors)})";
        }

        Message = summary;
        IsSuccess = res.Errors.Count == 0;
        return RedirectToPage(new { activeTab = "export-import" });
    }

    /// <summary>Resolves a relative or absolute path against issuer or current HTTP request.</summary>
    private string? ResolveEndpointUrl(string? relativeOrAbsoluteUri)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsoluteUri))
        {
            return null;
        }

        if (Uri.TryCreate(relativeOrAbsoluteUri, UriKind.Absolute, out var absUri))
        {
            return absUri.ToString();
        }

        var opt = GetServerOptions();
        if (opt?.Issuer is not null)
        {
            var baseUri = opt.Issuer.ToString().TrimEnd('/');
            var path = relativeOrAbsoluteUri.StartsWith('/') ? relativeOrAbsoluteUri : "/" + relativeOrAbsoluteUri;
            return $"{baseUri}{path}";
        }

        if (HttpContext?.Request is not null)
        {
            var scheme = HttpContext.Request.Scheme;
            var host = HttpContext.Request.Host.Value;
            var pathBase = HttpContext.Request.PathBase.Value?.TrimEnd('/') ?? string.Empty;
            var path = relativeOrAbsoluteUri.StartsWith('/') ? relativeOrAbsoluteUri : "/" + relativeOrAbsoluteUri;
            return $"{scheme}://{host}{pathBase}{path}";
        }

        return relativeOrAbsoluteUri;
    }
}

