using OpenIddict.Management.Enums;

namespace OpenIddict.Management.Dto;

/// <summary>
/// Serializable export format for a registered OpenIddict application.
/// </summary>
public sealed record ApplicationExportDto
{
    /// <summary>Gets the client identifier.</summary>
    public required string ClientId { get; init; }

    /// <summary>Gets the application display name.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets the application operational status.</summary>
    public ApplicationStatus Status { get; init; } = ApplicationStatus.Active;

    /// <summary>Gets the environment.</summary>
    public ApplicationEnvironment Environment { get; init; } = ApplicationEnvironment.Development;

    /// <summary>Gets the client type (confidential or public).</summary>
    public string? ClientType { get; init; }

    /// <summary>Gets the description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets allowed redirect URIs.</summary>
    public IReadOnlyList<string> RedirectUris { get; init; } = [];

    /// <summary>Gets allowed post-logout redirect URIs.</summary>
    public IReadOnlyList<string> PostLogoutRedirectUris { get; init; } = [];

    /// <summary>Gets permissions.</summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];

    /// <summary>Gets default scopes.</summary>
    public IReadOnlyList<string> DefaultScopes { get; init; } = [];

    /// <summary>Gets allowed roles.</summary>
    public IReadOnlyList<string> AllowedRoles { get; init; } = [];

    /// <summary>Gets tags.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Gets requirements.</summary>
    public IReadOnlyList<string> Requirements { get; init; } = [];

    /// <summary>Gets extra metadata.</summary>
    public string? ExtraData { get; init; }
}

/// <summary>
/// Serializable export format for an OpenID Connect scope.
/// </summary>
public sealed record ScopeExportDto
{
    /// <summary>Gets the scope name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the display name.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets the description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets associated resources.</summary>
    public IReadOnlyList<string> Resources { get; init; } = [];
}

/// <summary>
/// Serializable configuration export for the automatic token cleanup background job.
/// </summary>
public sealed record TokenCleanupConfigurationDto
{
    /// <summary>Gets a value indicating whether the token cleanup background job is active.</summary>
    public bool IsEnabled { get; init; }

    /// <summary>Gets the maximum amount of expired and revoked tokens to prune per execution cycle.</summary>
    public int BatchSize { get; init; } = 100;

    /// <summary>Gets the execution frequency for the cleanup background job.</summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(24);

    /// <summary>Gets a value indicating whether revoked tokens should be cleared in addition to expired tokens.</summary>
    public bool IncludeRevoked { get; init; } = true;
}

/// <summary>
/// Serializable configuration export for OpenIddict Management suite runtime options.
/// </summary>
public sealed record OpenIddictManagementConfigurationDto
{
    /// <summary>Gets the HTTP API route prefix.</summary>
    public string RoutePrefix { get; init; } = "/api/management";

    /// <summary>Gets a value indicating whether endpoints require HTTPS transport security.</summary>
    public bool RequireHttps { get; init; } = true;

    /// <summary>Gets a value indicating whether audit logging and event dispatching are enabled.</summary>
    public bool EnableAuditLogging { get; init; } = true;

    /// <summary>Gets the background token cleanup job configuration, if available.</summary>
    public TokenCleanupConfigurationDto? TokenCleanup { get; init; }
}

/// <summary>
/// Serializable configuration export for OpenIddict Server runtime options.
/// </summary>
public sealed record OpenIddictServerConfigurationDto
{
    /// <summary>Gets the server issuer URI string.</summary>
    public string? Issuer { get; init; }

    // --- Endpoints ---
    /// <summary>Gets authorization endpoint URIs.</summary>
    public IReadOnlyList<string> AuthorizationEndpointUris { get; init; } = [];

    /// <summary>Gets token endpoint URIs.</summary>
    public IReadOnlyList<string> TokenEndpointUris { get; init; } = [];

    /// <summary>Gets logout endpoint URIs.</summary>
    public IReadOnlyList<string> LogoutEndpointUris { get; init; } = [];

    /// <summary>Gets userinfo endpoint URIs.</summary>
    public IReadOnlyList<string> UserinfoEndpointUris { get; init; } = [];

    /// <summary>Gets introspection endpoint URIs.</summary>
    public IReadOnlyList<string> IntrospectionEndpointUris { get; init; } = [];

    /// <summary>Gets revocation endpoint URIs.</summary>
    public IReadOnlyList<string> RevocationEndpointUris { get; init; } = [];

    /// <summary>Gets device endpoint URIs.</summary>
    public IReadOnlyList<string> DeviceEndpointUris { get; init; } = [];

    /// <summary>Gets verification endpoint URIs.</summary>
    public IReadOnlyList<string> VerificationEndpointUris { get; init; } = [];

    /// <summary>Gets cryptography endpoint URIs.</summary>
    public IReadOnlyList<string> CryptographyEndpointUris { get; init; } = [];

    /// <summary>Gets configuration discovery endpoint URIs.</summary>
    public IReadOnlyList<string> ConfigurationEndpointUris { get; init; } = [];

    // --- Lifetimes ---
    /// <summary>Gets the access token lifetime.</summary>
    public TimeSpan? AccessTokenLifetime { get; init; }

    /// <summary>Gets the refresh token lifetime.</summary>
    public TimeSpan? RefreshTokenLifetime { get; init; }

    /// <summary>Gets the authorization code lifetime.</summary>
    public TimeSpan? AuthorizationCodeLifetime { get; init; }

    /// <summary>Gets the identity token lifetime.</summary>
    public TimeSpan? IdentityTokenLifetime { get; init; }

    /// <summary>Gets the device code lifetime.</summary>
    public TimeSpan? DeviceCodeLifetime { get; init; }

    /// <summary>Gets the user code lifetime.</summary>
    public TimeSpan? UserCodeLifetime { get; init; }

    /// <summary>Gets the refresh token reuse leeway.</summary>
    public TimeSpan? RefreshTokenReuseLeeway { get; init; }

    // --- Flows, Types & Scopes ---
    /// <summary>Gets allowed grant types.</summary>
    public IReadOnlyList<string> GrantTypes { get; init; } = [];

    /// <summary>Gets supported response types.</summary>
    public IReadOnlyList<string> ResponseTypes { get; init; } = [];

    /// <summary>Gets supported response modes.</summary>
    public IReadOnlyList<string> ResponseModes { get; init; } = [];

    /// <summary>Gets registered scopes.</summary>
    public IReadOnlyList<string> Scopes { get; init; } = [];

    /// <summary>Gets supported claims.</summary>
    public IReadOnlyList<string> Claims { get; init; } = [];

    /// <summary>Gets supported code challenge methods.</summary>
    public IReadOnlyList<string> CodeChallengeMethods { get; init; } = [];

    /// <summary>Gets supported client assertion types.</summary>
    public IReadOnlyList<string> ClientAssertionTypes { get; init; } = [];

    /// <summary>Gets supported client authentication methods.</summary>
    public IReadOnlyList<string> ClientAuthenticationMethods { get; init; } = [];

    /// <summary>Gets supported subject types.</summary>
    public IReadOnlyList<string> SubjectTypes { get; init; } = [];

    // --- Device Flow Settings ---
    /// <summary>Gets the user code length for device authorization flow.</summary>
    public int? UserCodeLength { get; init; }

    /// <summary>Gets the user code display format.</summary>
    public string? UserCodeDisplayFormat { get; init; }

    /// <summary>Gets the user code charset characters.</summary>
    public IReadOnlyList<string> UserCodeCharset { get; init; } = [];

    // --- Security, Storage & Policy Flags ---
    /// <summary>Gets a value indicating whether Proof Key for Code Exchange is required.</summary>
    public bool RequireProofKeyForCodeExchange { get; init; }

    /// <summary>Gets a value indicating whether access token encryption is disabled.</summary>
    public bool DisableAccessTokenEncryption { get; init; }

    /// <summary>Gets a value indicating whether rolling refresh tokens are disabled.</summary>
    public bool DisableRollingRefreshTokens { get; init; }

    /// <summary>Gets a value indicating whether sliding refresh token expiration is disabled.</summary>
    public bool DisableSlidingRefreshTokenExpiration { get; init; }

    /// <summary>Gets a value indicating whether reference access tokens are used.</summary>
    public bool UseReferenceAccessTokens { get; init; }

    /// <summary>Gets a value indicating whether reference refresh tokens are used.</summary>
    public bool UseReferenceRefreshTokens { get; init; }

    /// <summary>Gets a value indicating whether token storage is disabled.</summary>
    public bool DisableTokenStorage { get; init; }

    /// <summary>Gets a value indicating whether authorization storage is disabled.</summary>
    public bool DisableAuthorizationStorage { get; init; }

    /// <summary>Gets a value indicating whether scope validation is disabled.</summary>
    public bool DisableScopeValidation { get; init; }

    /// <summary>Gets a value indicating whether anonymous clients are accepted.</summary>
    public bool AcceptAnonymousClients { get; init; }

    /// <summary>Gets a value indicating whether endpoint permissions are ignored.</summary>
    public bool IgnoreEndpointPermissions { get; init; }

    /// <summary>Gets a value indicating whether grant type permissions are ignored.</summary>
    public bool IgnoreGrantTypePermissions { get; init; }

    /// <summary>Gets a value indicating whether response type permissions are ignored.</summary>
    public bool IgnoreResponseTypePermissions { get; init; }

    /// <summary>Gets a value indicating whether scope permissions are ignored.</summary>
    public bool IgnoreScopePermissions { get; init; }

    /// <summary>Gets a value indicating whether degraded mode is enabled.</summary>
    public bool EnableDegradedMode { get; init; }
}

/// <summary>
/// Container package representing an export of OpenIddict Management configuration (applications, scopes, server options, and management settings).
/// </summary>
public sealed record ManagementExportPackage
{
    /// <summary>Gets the schema version of the export package.</summary>
    public string Version { get; init; } = "1.0";

    /// <summary>Gets the UTC timestamp when the package was exported.</summary>
    public DateTimeOffset ExportedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Gets the collection of exported applications.</summary>
    public IReadOnlyList<ApplicationExportDto> Applications { get; init; } = [];

    /// <summary>Gets the collection of exported scopes.</summary>
    public IReadOnlyList<ScopeExportDto> Scopes { get; init; } = [];

    /// <summary>Gets the exported OpenIddict server configuration, if available.</summary>
    public OpenIddictServerConfigurationDto? OpenIddictServer { get; init; }

    /// <summary>Gets the exported OpenIddict Management suite configuration, if available.</summary>
    public OpenIddictManagementConfigurationDto? Management { get; init; }
}

/// <summary>
/// Options controlling import behavior.
/// </summary>
public sealed record ImportOptions
{
    /// <summary>
    /// Gets a value indicating whether existing applications or scopes with matching identifiers should be updated. Defaults to false (skip existing).
    /// </summary>
    public bool OverwriteExisting { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether to import applications. Defaults to true.
    /// </summary>
    public bool ImportApplications { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to import scopes. Defaults to true.
    /// </summary>
    public bool ImportScopes { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to import OpenIddict server and management runtime configurations. Defaults to true.
    /// </summary>
    public bool ImportConfigurations { get; init; } = true;
}

/// <summary>
/// Summary result of an import operation.
/// </summary>
public sealed record ImportResultDto
{
    /// <summary>Gets count of newly created applications.</summary>
    public int ApplicationsCreated { get; init; }

    /// <summary>Gets count of updated existing applications.</summary>
    public int ApplicationsUpdated { get; init; }

    /// <summary>Gets count of skipped applications.</summary>
    public int ApplicationsSkipped { get; init; }

    /// <summary>Gets count of newly created scopes.</summary>
    public int ScopesCreated { get; init; }

    /// <summary>Gets count of updated existing scopes.</summary>
    public int ScopesUpdated { get; init; }

    /// <summary>Gets count of skipped scopes.</summary>
    public int ScopesSkipped { get; init; }

    /// <summary>Gets a value indicating whether OpenIddict server options were imported.</summary>
    public bool OpenIddictServerConfigImported { get; init; }

    /// <summary>Gets a value indicating whether OpenIddict management options were imported.</summary>
    public bool ManagementConfigImported { get; init; }

    /// <summary>Gets any errors encountered during import.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];
}
