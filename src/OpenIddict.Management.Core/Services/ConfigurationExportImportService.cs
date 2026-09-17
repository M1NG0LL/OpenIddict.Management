using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Options;
using OpenIddict.Management.Results;
using OpenIddict.Server;

namespace OpenIddict.Management.Services;

/// <summary>
/// Implementation of <see cref="IConfigurationExportImportService"/> for exporting and importing configuration packages.
/// </summary>
public sealed class ConfigurationExportImportService(
    IApplicationManagementService applicationService,
    IScopeManagementService scopeService,
    IOptions<OpenIddictManagementOptions>? managementOptions = null,
    ITokenCleanupJobManager? cleanupJobManager = null,
    IOptions<TokenCleanupOptions>? tokenCleanupOptions = null,
    IOptionsMonitor<OpenIddictServerOptions>? serverOptionsMonitor = null,
    IOptions<OpenIddictServerOptions>? serverOptions = null,
    ILogger<ConfigurationExportImportService>? logger = null) : IConfigurationExportImportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private OpenIddictServerOptions? GetServerOptions() =>
        serverOptionsMonitor?.CurrentValue ?? serverOptions?.Value;

    /// <inheritdoc/>
    public async Task<Result<ManagementExportPackage>> ExportConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var appListResult = await applicationService.ListAsync(new PagedRequest { PageIndex = 1, PageSize = 100 }, cancellationToken: cancellationToken);
        if (appListResult.IsFailure)
        {
            return Result.Failure<ManagementExportPackage>(appListResult.Error ?? ManagementError.Custom("ExportError", "Failed to list applications."));
        }

        var exportedApps = new List<ApplicationExportDto>();
        foreach (var appSummary in appListResult.Value.Items)
        {
            var detailResult = await applicationService.GetByIdAsync(appSummary.Id, cancellationToken);
            if (detailResult.IsSuccess && detailResult.Value is not null)
            {
                var app = detailResult.Value;
                exportedApps.Add(new ApplicationExportDto
                {
                    ClientId = app.ClientId,
                    DisplayName = app.DisplayName,
                    Status = app.Status,
                    Environment = app.Environment,
                    ClientType = app.ClientType,
                    Description = app.Description,
                    RedirectUris = app.RedirectUris,
                    PostLogoutRedirectUris = app.PostLogoutRedirectUris,
                    Permissions = app.Permissions,
                    DefaultScopes = app.DefaultScopes,
                    AllowedRoles = app.AllowedRoles,
                    Tags = app.Tags,
                    Requirements = app.Requirements,
                    ExtraData = app.ExtraData
                });
            }
        }

        var scopeListResult = await scopeService.ListAsync(new PagedRequest { PageIndex = 1, PageSize = 100 }, cancellationToken);
        if (scopeListResult.IsFailure)
        {
            return Result.Failure<ManagementExportPackage>(scopeListResult.Error ?? ManagementError.Custom("ExportError", "Failed to list scopes."));
        }

        var exportedScopes = scopeListResult.Value.Items.Select(s => new ScopeExportDto
        {
            Name = s.Name,
            DisplayName = s.DisplayName,
            Description = s.Description,
            Resources = s.Resources
        }).ToList();

        // Export OpenIddict Server Options
        OpenIddictServerConfigurationDto? serverConfig = null;
        var sOpt = GetServerOptions();
        if (sOpt is not null)
        {
            serverConfig = new OpenIddictServerConfigurationDto
            {
                Issuer = sOpt.Issuer?.OriginalString ?? sOpt.Issuer?.ToString(),
                AuthorizationEndpointUris = sOpt.AuthorizationEndpointUris.Select(u => u.OriginalString).ToList(),
                TokenEndpointUris = sOpt.TokenEndpointUris.Select(u => u.OriginalString).ToList(),
                LogoutEndpointUris = sOpt.LogoutEndpointUris.Select(u => u.OriginalString).ToList(),
                UserinfoEndpointUris = sOpt.UserinfoEndpointUris.Select(u => u.OriginalString).ToList(),
                IntrospectionEndpointUris = sOpt.IntrospectionEndpointUris.Select(u => u.OriginalString).ToList(),
                RevocationEndpointUris = sOpt.RevocationEndpointUris.Select(u => u.OriginalString).ToList(),
                DeviceEndpointUris = sOpt.DeviceEndpointUris.Select(u => u.OriginalString).ToList(),
                VerificationEndpointUris = sOpt.VerificationEndpointUris.Select(u => u.OriginalString).ToList(),
                CryptographyEndpointUris = sOpt.CryptographyEndpointUris.Select(u => u.OriginalString).ToList(),
                ConfigurationEndpointUris = sOpt.ConfigurationEndpointUris.Select(u => u.OriginalString).ToList(),

                AccessTokenLifetime = sOpt.AccessTokenLifetime,
                RefreshTokenLifetime = sOpt.RefreshTokenLifetime,
                AuthorizationCodeLifetime = sOpt.AuthorizationCodeLifetime,
                IdentityTokenLifetime = sOpt.IdentityTokenLifetime,
                DeviceCodeLifetime = sOpt.DeviceCodeLifetime,
                UserCodeLifetime = sOpt.UserCodeLifetime,
                RefreshTokenReuseLeeway = sOpt.RefreshTokenReuseLeeway,

                GrantTypes = sOpt.GrantTypes.ToList(),
                ResponseTypes = sOpt.ResponseTypes.ToList(),
                ResponseModes = sOpt.ResponseModes.ToList(),
                Scopes = sOpt.Scopes.ToList(),
                Claims = sOpt.Claims.ToList(),
                CodeChallengeMethods = sOpt.CodeChallengeMethods.ToList(),
                ClientAssertionTypes = sOpt.ClientAssertionTypes.ToList(),
                ClientAuthenticationMethods = sOpt.ClientAuthenticationMethods.ToList(),
                SubjectTypes = sOpt.SubjectTypes.ToList(),

                UserCodeLength = sOpt.UserCodeLength,
                UserCodeDisplayFormat = sOpt.UserCodeDisplayFormat,
                UserCodeCharset = sOpt.UserCodeCharset.ToList(),

                RequireProofKeyForCodeExchange = sOpt.RequireProofKeyForCodeExchange,
                DisableAccessTokenEncryption = sOpt.DisableAccessTokenEncryption,
                DisableRollingRefreshTokens = sOpt.DisableRollingRefreshTokens,
                DisableSlidingRefreshTokenExpiration = sOpt.DisableSlidingRefreshTokenExpiration,
                UseReferenceAccessTokens = sOpt.UseReferenceAccessTokens,
                UseReferenceRefreshTokens = sOpt.UseReferenceRefreshTokens,
                DisableTokenStorage = sOpt.DisableTokenStorage,
                DisableAuthorizationStorage = sOpt.DisableAuthorizationStorage,
                DisableScopeValidation = sOpt.DisableScopeValidation,
                AcceptAnonymousClients = sOpt.AcceptAnonymousClients,
                IgnoreEndpointPermissions = sOpt.IgnoreEndpointPermissions,
                IgnoreGrantTypePermissions = sOpt.IgnoreGrantTypePermissions,
                IgnoreResponseTypePermissions = sOpt.IgnoreResponseTypePermissions,
                IgnoreScopePermissions = sOpt.IgnoreScopePermissions,
                EnableDegradedMode = sOpt.EnableDegradedMode
            };
        }

        // Export OpenIddict Management Options
        OpenIddictManagementConfigurationDto? managementConfig = null;
        var mOpt = managementOptions?.Value;
        TokenCleanupConfigurationDto? cleanupConfig = null;
        if (cleanupJobManager is not null)
        {
            cleanupConfig = new TokenCleanupConfigurationDto
            {
                IsEnabled = cleanupJobManager.IsEnabled,
                BatchSize = cleanupJobManager.BatchSize,
                Interval = cleanupJobManager.Interval,
                IncludeRevoked = cleanupJobManager.IncludeRevoked
            };
        }
        else if (tokenCleanupOptions?.Value is not null)
        {
            cleanupConfig = new TokenCleanupConfigurationDto
            {
                IsEnabled = tokenCleanupOptions.Value.IsEnabled,
                BatchSize = tokenCleanupOptions.Value.BatchSize,
                Interval = tokenCleanupOptions.Value.Interval,
                IncludeRevoked = tokenCleanupOptions.Value.IncludeRevoked
            };
        }

        if (mOpt is not null || cleanupConfig is not null)
        {
            managementConfig = new OpenIddictManagementConfigurationDto
            {
                RoutePrefix = mOpt?.RoutePrefix ?? "/api/management",
                RequireHttps = mOpt?.RequireHttps ?? true,
                EnableAuditLogging = mOpt?.EnableAuditLogging ?? true,
                TokenCleanup = cleanupConfig
            };
        }

        var package = new ManagementExportPackage
        {
            Version = "1.0",
            ExportedAt = DateTimeOffset.UtcNow,
            Applications = exportedApps,
            Scopes = exportedScopes,
            OpenIddictServer = serverConfig,
            Management = managementConfig
        };

        return package;
    }

    /// <inheritdoc/>
    public async Task<Result<string>> ExportConfigurationAsJsonAsync(CancellationToken cancellationToken = default)
    {
        var packageResult = await ExportConfigurationAsync(cancellationToken);
        if (packageResult.IsFailure)
        {
            return Result.Failure<string>(packageResult.Error ?? ManagementError.Custom("ExportError", "Failed to export configuration."));
        }

        try
        {
            var json = JsonSerializer.Serialize(packageResult.Value, JsonOptions);
            return json;
        }
        catch (Exception ex)
        {
            return Result.Failure<string>("SerializationError", $"Failed to serialize export package: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ImportResultDto>> ImportConfigurationAsync(
        ManagementExportPackage package,
        ImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (package is null)
        {
            return Result.Failure<ImportResultDto>("ValidationError", "Export package cannot be null.");
        }

        options ??= new ImportOptions();

        var appsCreated = 0;
        var appsUpdated = 0;
        var appsSkipped = 0;
        var scopesCreated = 0;
        var scopesUpdated = 0;
        var scopesSkipped = 0;
        var serverConfigImported = false;
        var managementConfigImported = false;
        var errors = new List<string>();

        // 1. Scopes
        if (options.ImportScopes && package.Scopes is { Count: > 0 })
        {
            foreach (var scopeDto in package.Scopes)
            {
                if (string.IsNullOrWhiteSpace(scopeDto.Name))
                {
                    continue;
                }

                try
                {
                    var existingResult = await scopeService.GetByNameAsync(scopeDto.Name, cancellationToken);
                    if (existingResult.IsSuccess && existingResult.Value is not null)
                    {
                        if (options.OverwriteExisting)
                        {
                            var updateResult = await scopeService.UpdateAsync(existingResult.Value.Id, new UpdateScopeRequest
                            {
                                DisplayName = scopeDto.DisplayName,
                                Description = scopeDto.Description,
                                Resources = scopeDto.Resources?.ToList()
                            }, cancellationToken);

                            if (updateResult.IsSuccess)
                            {
                                scopesUpdated++;
                            }
                            else
                            {
                                errors.Add($"Scope '{scopeDto.Name}' update failed: {updateResult.Error?.Description ?? "Update failed"}");
                            }
                        }
                        else
                        {
                            scopesSkipped++;
                        }
                    }
                    else
                    {
                        var createResult = await scopeService.CreateAsync(new CreateScopeRequest
                        {
                            Name = scopeDto.Name,
                            DisplayName = scopeDto.DisplayName,
                            Description = scopeDto.Description,
                            Resources = scopeDto.Resources?.ToList()
                        }, cancellationToken);

                        if (createResult.IsSuccess)
                        {
                            scopesCreated++;
                        }
                        else
                        {
                            errors.Add($"Scope '{scopeDto.Name}' creation failed: {createResult.Error?.Description ?? "Creation failed"}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error processing scope '{scopeDto.Name}': {ex.Message}");
                }
            }
        }

        // 2. Applications
        if (options.ImportApplications && package.Applications is { Count: > 0 })
        {
            foreach (var appDto in package.Applications)
            {
                if (string.IsNullOrWhiteSpace(appDto.ClientId))
                {
                    continue;
                }

                try
                {
                    var existingResult = await applicationService.GetByClientIdAsync(appDto.ClientId, cancellationToken);
                    if (existingResult.IsSuccess && existingResult.Value is not null)
                    {
                        if (options.OverwriteExisting)
                        {
                            var updateResult = await applicationService.UpdateAsync(existingResult.Value.Id, new ApplicationUpdateDto
                            {
                                DisplayName = appDto.DisplayName ?? appDto.ClientId,
                                Environment = appDto.Environment,
                                Status = appDto.Status,
                                ClientType = appDto.ClientType,
                                Description = appDto.Description,
                                ExtraData = appDto.ExtraData,
                                Tags = appDto.Tags?.ToList() ?? [],
                                AllowedRoles = appDto.AllowedRoles?.ToList() ?? [],
                                RedirectUris = appDto.RedirectUris?.ToList() ?? [],
                                PostLogoutRedirectUris = appDto.PostLogoutRedirectUris?.ToList() ?? [],
                                Permissions = appDto.Permissions?.ToList() ?? [],
                                DefaultScopes = appDto.DefaultScopes?.ToList() ?? [],
                                Requirements = appDto.Requirements?.ToList() ?? []
                            }, cancellationToken);

                            if (updateResult.IsSuccess)
                            {
                                appsUpdated++;
                            }
                            else
                            {
                                errors.Add($"Application '{appDto.ClientId}' update failed: {updateResult.Error?.Description ?? "Update failed"}");
                            }
                        }
                        else
                        {
                            appsSkipped++;
                        }
                    }
                    else
                    {
                        var createResult = await applicationService.CreateAsync(new ApplicationCreateDto
                        {
                            ClientId = appDto.ClientId,
                            DisplayName = appDto.DisplayName ?? appDto.ClientId,
                            Environment = appDto.Environment,
                            ClientType = appDto.ClientType,
                            Description = appDto.Description,
                            ExtraData = appDto.ExtraData,
                            Tags = appDto.Tags?.ToList() ?? [],
                            AllowedRoles = appDto.AllowedRoles?.ToList() ?? [],
                            RedirectUris = appDto.RedirectUris?.ToList() ?? [],
                            PostLogoutRedirectUris = appDto.PostLogoutRedirectUris?.ToList() ?? [],
                            Permissions = appDto.Permissions?.ToList() ?? [],
                            DefaultScopes = appDto.DefaultScopes?.ToList() ?? [],
                            Requirements = appDto.Requirements?.ToList() ?? []
                        }, cancellationToken);

                        if (createResult.IsSuccess)
                        {
                            appsCreated++;
                            if (appDto.Status != ApplicationStatus.Active)
                            {
                                await applicationService.UpdateStatusAsync(createResult.Value.Id, appDto.Status, cancellationToken);
                            }
                        }
                        else
                        {
                            errors.Add($"Application '{appDto.ClientId}' creation failed: {createResult.Error?.Description ?? "Creation failed"}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error processing application '{appDto.ClientId}': {ex.Message}");
                }
            }
        }

        // 3. Configurations
        if (options.ImportConfigurations)
        {
            // 3a. OpenIddict Server Options
            if (package.OpenIddictServer is not null)
            {
                var sOpt = GetServerOptions();
                if (sOpt is not null)
                {
                    try
                    {
                        ApplyServerConfiguration(sOpt, package.OpenIddictServer);
                        serverConfigImported = true;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to import OpenIddict server configuration: {ex.Message}");
                        logger?.LogError(ex, "Failed to apply imported OpenIddict server configuration.");
                    }
                }
                else
                {
                    errors.Add("Cannot import OpenIddict server configuration: OpenIddictServerOptions is not configured in this application.");
                }
            }

            // 3b. OpenIddict Management Options
            if (package.Management is not null)
            {
                try
                {
                    ApplyManagementConfiguration(package.Management);
                    managementConfigImported = true;
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to import OpenIddict management configuration: {ex.Message}");
                    logger?.LogError(ex, "Failed to apply imported OpenIddict management configuration.");
                }
            }
        }

        return new ImportResultDto
        {
            ApplicationsCreated = appsCreated,
            ApplicationsUpdated = appsUpdated,
            ApplicationsSkipped = appsSkipped,
            ScopesCreated = scopesCreated,
            ScopesUpdated = scopesUpdated,
            ScopesSkipped = scopesSkipped,
            OpenIddictServerConfigImported = serverConfigImported,
            ManagementConfigImported = managementConfigImported,
            Errors = errors
        };
    }

    /// <inheritdoc/>
    public Task<Result<ImportResultDto>> ImportConfigurationFromJsonAsync(
        string json,
        ImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult(Result.Failure<ImportResultDto>("ValidationError", "JSON text cannot be empty."));
        }

        try
        {
            var package = JsonSerializer.Deserialize<ManagementExportPackage>(json, JsonOptions);
            if (package is null)
            {
                return Task.FromResult(Result.Failure<ImportResultDto>("ValidationError", "Failed to deserialize JSON into export package."));
            }

            return ImportConfigurationAsync(package, options, cancellationToken);
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<ImportResultDto>("JsonError", $"Invalid JSON format: {ex.Message}"));
        }
    }

    private static void ApplyServerConfiguration(OpenIddictServerOptions target, OpenIddictServerConfigurationDto source)
    {
        target.Issuer = string.IsNullOrWhiteSpace(source.Issuer)
            ? null
            : new Uri(source.Issuer, UriKind.RelativeOrAbsolute);

        ReplaceUris(target.AuthorizationEndpointUris, source.AuthorizationEndpointUris);
        ReplaceUris(target.TokenEndpointUris, source.TokenEndpointUris);
        ReplaceUris(target.LogoutEndpointUris, source.LogoutEndpointUris);
        ReplaceUris(target.UserinfoEndpointUris, source.UserinfoEndpointUris);
        ReplaceUris(target.IntrospectionEndpointUris, source.IntrospectionEndpointUris);
        ReplaceUris(target.RevocationEndpointUris, source.RevocationEndpointUris);
        ReplaceUris(target.DeviceEndpointUris, source.DeviceEndpointUris);
        ReplaceUris(target.VerificationEndpointUris, source.VerificationEndpointUris);
        ReplaceUris(target.CryptographyEndpointUris, source.CryptographyEndpointUris);
        ReplaceUris(target.ConfigurationEndpointUris, source.ConfigurationEndpointUris);

        target.AccessTokenLifetime = source.AccessTokenLifetime;
        target.RefreshTokenLifetime = source.RefreshTokenLifetime;
        target.AuthorizationCodeLifetime = source.AuthorizationCodeLifetime;
        target.IdentityTokenLifetime = source.IdentityTokenLifetime;
        target.DeviceCodeLifetime = source.DeviceCodeLifetime;
        target.UserCodeLifetime = source.UserCodeLifetime;
        target.RefreshTokenReuseLeeway = source.RefreshTokenReuseLeeway;

        ReplaceItems(target.GrantTypes, source.GrantTypes);
        ReplaceItems(target.ResponseTypes, source.ResponseTypes);
        ReplaceItems(target.ResponseModes, source.ResponseModes);
        ReplaceItems(target.Scopes, source.Scopes);
        ReplaceItems(target.Claims, source.Claims);
        ReplaceItems(target.CodeChallengeMethods, source.CodeChallengeMethods);
        ReplaceItems(target.ClientAssertionTypes, source.ClientAssertionTypes);
        ReplaceItems(target.ClientAuthenticationMethods, source.ClientAuthenticationMethods);
        ReplaceItems(target.SubjectTypes, source.SubjectTypes);
        ReplaceItems(target.UserCodeCharset, source.UserCodeCharset);

        if (source.UserCodeLength.HasValue)
        {
            target.UserCodeLength = source.UserCodeLength.Value;
        }
        if (source.UserCodeDisplayFormat is not null)
        {
            target.UserCodeDisplayFormat = source.UserCodeDisplayFormat;
        }

        target.RequireProofKeyForCodeExchange = source.RequireProofKeyForCodeExchange;
        target.DisableAccessTokenEncryption = source.DisableAccessTokenEncryption;
        target.DisableRollingRefreshTokens = source.DisableRollingRefreshTokens;
        target.DisableSlidingRefreshTokenExpiration = source.DisableSlidingRefreshTokenExpiration;
        target.UseReferenceAccessTokens = source.UseReferenceAccessTokens;
        target.UseReferenceRefreshTokens = source.UseReferenceRefreshTokens;
        target.DisableTokenStorage = source.DisableTokenStorage;
        target.DisableAuthorizationStorage = source.DisableAuthorizationStorage;
        target.DisableScopeValidation = source.DisableScopeValidation;
        target.AcceptAnonymousClients = source.AcceptAnonymousClients;
        target.IgnoreEndpointPermissions = source.IgnoreEndpointPermissions;
        target.IgnoreGrantTypePermissions = source.IgnoreGrantTypePermissions;
        target.IgnoreResponseTypePermissions = source.IgnoreResponseTypePermissions;
        target.IgnoreScopePermissions = source.IgnoreScopePermissions;
        target.EnableDegradedMode = source.EnableDegradedMode;
    }

    private void ApplyManagementConfiguration(OpenIddictManagementConfigurationDto source)
    {
        if (managementOptions?.Value is not null)
        {
            if (!string.IsNullOrWhiteSpace(source.RoutePrefix))
            {
                managementOptions.Value.RoutePrefix = source.RoutePrefix;
            }
            managementOptions.Value.RequireHttps = source.RequireHttps;
            managementOptions.Value.EnableAuditLogging = source.EnableAuditLogging;
        }

        if (source.TokenCleanup is not null)
        {
            cleanupJobManager?.UpdateSettings(
                isEnabled: source.TokenCleanup.IsEnabled,
                batchSize: source.TokenCleanup.BatchSize,
                interval: source.TokenCleanup.Interval,
                includeRevoked: source.TokenCleanup.IncludeRevoked);

            if (tokenCleanupOptions?.Value is not null)
            {
                tokenCleanupOptions.Value.IsEnabled = source.TokenCleanup.IsEnabled;
                tokenCleanupOptions.Value.BatchSize = source.TokenCleanup.BatchSize;
                tokenCleanupOptions.Value.Interval = source.TokenCleanup.Interval;
                tokenCleanupOptions.Value.IncludeRevoked = source.TokenCleanup.IncludeRevoked;
            }
        }
    }

    private static void ReplaceUris(ICollection<Uri> collection, IReadOnlyList<string>? uris)
    {
        if (uris is null) return;
        collection.Clear();
        foreach (var uriStr in uris)
        {
            if (!string.IsNullOrWhiteSpace(uriStr))
            {
                collection.Add(new Uri(uriStr, UriKind.RelativeOrAbsolute));
            }
        }
    }

    private static void ReplaceItems(ICollection<string> collection, IReadOnlyList<string>? items)
    {
        if (items is null) return;
        collection.Clear();
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item))
            {
                collection.Add(item.Trim());
            }
        }
    }
}
