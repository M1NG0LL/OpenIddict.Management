using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenIddict.Abstractions;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dashboard.Pages.Settings;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Options;
using OpenIddict.Management.Results;
using OpenIddict.Server;
using Xunit;

namespace OpenIddict.Management.Dashboard.Tests;

public class SettingsPageTests
{
    private static (IndexModel model, ITokenCleanupJobManager cleanupJobManager) CreateModel()
    {
        var cleanupJobManager = Substitute.For<ITokenCleanupJobManager>();
        cleanupJobManager.IsEnabled.Returns(true);
        cleanupJobManager.BatchSize.Returns(200);
        cleanupJobManager.Interval.Returns(TimeSpan.FromHours(2));
        cleanupJobManager.IncludeRevoked.Returns(true);
        cleanupJobManager.LastRunTime.Returns(DateTimeOffset.UtcNow);
        cleanupJobManager.LastPrunedCount.Returns(18);
        cleanupJobManager.TotalPrunedCount.Returns(90);
        cleanupJobManager.LastStatus.Returns("Idle");

        var dashboardOptions = Microsoft.Extensions.Options.Options.Create(new DashboardOptions
        {
            DashboardTitle = "Custom Identity Admin",
            PathPrefix = "/admin/id",
            ExitUrl = "/logout",
            RequireAuthorization = true,
            AuthorizationPolicy = "AdminOnly"
        });

        var model = new IndexModel(cleanupJobManager, dashboardOptions)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Substitute.For<ITempDataProvider>())
        };

        return (model, cleanupJobManager);
    }

    [Fact]
    public async Task OnGet_PopulatesTokensTabPropertiesAndGeneralSettings()
    {
        var (model, _) = CreateModel();

        await model.OnGetAsync();

        // Token tab properties
        model.CleanupJobEnabled.Should().BeTrue();
        model.CleanupJobBatchSize.Should().Be(200);
        model.CleanupJobIntervalMinutes.Should().Be(120);
        model.CleanupJobIncludeRevoked.Should().BeTrue();
        model.CleanupJobLastRun.Should().NotBeNull();
        model.CleanupJobLastPrunedCount.Should().Be(18);
        model.CleanupJobTotalPrunedCount.Should().Be(90);
        model.CleanupJobStatus.Should().Be("Idle");

        // General tab properties
        model.DashboardTitle.Should().Be("Custom Identity Admin");
        model.PathPrefix.Should().Be("/admin/id");
        model.ExitUrl.Should().Be("/logout");
        model.RequireAuthorization.Should().BeTrue();
        model.AuthorizationPolicy.Should().Be("AdminOnly");
    }

    [Fact]
    public void OnPostToggleCleanupJob_TogglesJobAndRedirectsToTokensTab()
    {
        var (model, cleanupJobManager) = CreateModel();

        var result = model.OnPostToggleCleanupJob();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.RouteValues.Should().NotBeNull();
        redirect.RouteValues!["activeTab"].Should().Be("tokens");

        cleanupJobManager.Received(1).SetEnabled(false);
        model.IsSuccess.Should().BeTrue();
        model.Message.Should().Contain("paused");
    }

    [Fact]
    public void OnPostUpdateCleanupJobSettings_ValidAmount_UpdatesSettingsAndRedirects()
    {
        var (model, cleanupJobManager) = CreateModel();

        var result = model.OnPostUpdateCleanupJobSettings(batchSize: 350, intervalMinutes: 45, includeRevoked: false);

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.RouteValues.Should().NotBeNull();
        redirect.RouteValues!["activeTab"].Should().Be("tokens");

        cleanupJobManager.Received(1).UpdateSettings(
            isEnabled: null,
            batchSize: 350,
            interval: TimeSpan.FromMinutes(45),
            includeRevoked: false);

        model.IsSuccess.Should().BeTrue();
        model.Message.Should().Contain("350");
    }

    [Fact]
    public void OnPostUpdateCleanupJobSettings_InvalidAmount_Fails()
    {
        var (model, cleanupJobManager) = CreateModel();

        var result = model.OnPostUpdateCleanupJobSettings(batchSize: -5);

        result.Should().BeOfType<RedirectToPageResult>();
        cleanupJobManager.DidNotReceive().UpdateSettings(
            Arg.Any<bool?>(),
            Arg.Any<int?>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<bool?>());

        model.IsSuccess.Should().BeFalse();
        model.Message.Should().Contain("greater than zero");
    }

    [Fact]
    public async Task OnPostRunCleanupJobNowAsync_TriggersImmediateRunAndRedirects()
    {
        var (model, cleanupJobManager) = CreateModel();
        cleanupJobManager.TriggerRunAsync(Arg.Any<CancellationToken>())
            .Returns(33);

        var result = await model.OnPostRunCleanupJobNowAsync(CancellationToken.None);

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.RouteValues.Should().NotBeNull();
        redirect.RouteValues!["activeTab"].Should().Be("tokens");

        await cleanupJobManager.Received(1).TriggerRunAsync(Arg.Any<CancellationToken>());
        model.IsSuccess.Should().BeTrue();
        model.Message.Should().Contain("33");
    }

    [Fact]
    public async Task OnGet_WithServerOptions_PopulatesOidcPropertiesAndEndpoints()
    {
        var cleanupJobManager = Substitute.For<ITokenCleanupJobManager>();
        var dashboardOptions = Microsoft.Extensions.Options.Options.Create(new DashboardOptions());
        var serverOptions = new OpenIddictServerOptions
        {
            Issuer = new Uri("https://id.example.com/"),
            AccessTokenLifetime = TimeSpan.FromMinutes(45),
            RefreshTokenLifetime = TimeSpan.FromDays(30),
            AuthorizationCodeLifetime = TimeSpan.FromMinutes(10),
            RequireProofKeyForCodeExchange = true,
            UseReferenceAccessTokens = true,
            UseReferenceRefreshTokens = true
        };
        serverOptions.AuthorizationEndpointUris.Add(new Uri("/connect/authorize", UriKind.Relative));
        serverOptions.TokenEndpointUris.Add(new Uri("/connect/token", UriKind.Relative));
        serverOptions.ConfigurationEndpointUris.Add(new Uri("/.well-known/openid-configuration", UriKind.Relative));
        serverOptions.GrantTypes.Add(OpenIddictConstants.GrantTypes.AuthorizationCode);
        serverOptions.GrantTypes.Add(OpenIddictConstants.GrantTypes.RefreshToken);
        serverOptions.Scopes.Add(OpenIddictConstants.Scopes.OpenId);
        serverOptions.Scopes.Add(OpenIddictConstants.Scopes.Profile);
        serverOptions.Scopes.Add("api");

        var serverOptionsWrapper = Microsoft.Extensions.Options.Options.Create(serverOptions);

        var model = new IndexModel(
            cleanupJobManager: cleanupJobManager,
            dashboardOptions: dashboardOptions,
            serverOptions: serverOptionsWrapper)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Substitute.For<ITempDataProvider>())
        };

        await model.OnGetAsync();

        model.IsServerConfigured.Should().BeTrue();
        model.OidcIssuer.Should().Be("https://id.example.com/");
        model.AuthorizationEndpointUris.Should().Contain("/connect/authorize");
        model.TokenEndpointUris.Should().Contain("/connect/token");
        model.DiscoveryEndpointUrl.Should().Be("https://id.example.com/.well-known/openid-configuration");
        model.AllowAuthorizationCodeFlow.Should().BeTrue();
        model.AllowRefreshTokenFlow.Should().BeTrue();
        model.AllowClientCredentialsFlow.Should().BeFalse();
        model.AccessTokenLifetimeMinutes.Should().Be(45);
        model.RefreshTokenLifetimeDays.Should().Be(30);
        model.AuthorizationCodeLifetimeMinutes.Should().Be(10);
        model.RequireProofKeyForCodeExchange.Should().BeTrue();
        model.UseReferenceAccessTokens.Should().BeTrue();
        model.SelectedRegisteredScopes.Should().Contain([OpenIddictConstants.Scopes.OpenId, OpenIddictConstants.Scopes.Profile, "api"]);
        model.AccessTokenLifetimeDescription.Should().Contain("45 mins");
        model.RefreshTokenLifetimeDescription.Should().Contain("30 days");
    }

    [Fact]
    public async Task OnGet_WithoutServerOptions_GracefullyMarksNotConfigured()
    {
        var (model, _) = CreateModel();

        await model.OnGetAsync();

        model.IsServerConfigured.Should().BeFalse();
        model.AuthorizationEndpointUris.Should().BeEmpty();
        model.DiscoveryEndpointUrl.Should().BeNull();
    }

    [Fact]
    public void OnPostUpdateOidcSettings_UpdatesServerOptionsAndRedirectsToOidcTab()
    {
        var serverOptions = new OpenIddictServerOptions();
        var serverOptionsWrapper = Microsoft.Extensions.Options.Options.Create(serverOptions);

        var model = new IndexModel(
            cleanupJobManager: null,
            dashboardOptions: null,
            serverOptions: serverOptionsWrapper)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Substitute.For<ITempDataProvider>())
        };

        var result = model.OnPostUpdateOidcSettings(
            allowAuthCode: true,
            allowClientCreds: true,
            allowRefreshToken: true,
            customGrantTypes: "custom_flow, urn:custom:flow",
            accessTokenMinutes: 120,
            refreshTokenDays: 60,
            authCodeMinutes: 15,
            identityTokenMinutes: 30,
            deviceCodeMinutes: 20,
            reuseLeewaySeconds: 10,
            selectedScopes: [OpenIddictConstants.Scopes.OpenId, OpenIddictConstants.Scopes.Email],
            customScopesText: "custom_scope1, custom_scope2",
            requirePkce: true,
            disableTokenEncryption: true,
            disableRollingRefresh: true,
            disableSlidingRefresh: true,
            useRefAccessTokens: true,
            useRefRefreshTokens: true,
            disableTokenStorage: false,
            disableAuthStorage: false,
            disableScopeValidation: false,
            acceptAnonymousClients: true,
            ignoreEndpointPermissions: false,
            ignoreGrantTypePermissions: false,
            ignoreResponseTypePermissions: false,
            ignoreScopePermissions: false,
            enableDegradedMode: false,
            userCodeLength: 8,
            userCodeDisplayFormat: "****-****");

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.RouteValues.Should().NotBeNull();
        redirect.RouteValues!["activeTab"].Should().Be("oidc");

        model.IsSuccess.Should().BeTrue();
        model.Message.Should().Contain("successfully");

        // Validate mutated server options
        serverOptions.GrantTypes.Should().Contain([
            OpenIddictConstants.GrantTypes.AuthorizationCode,
            OpenIddictConstants.GrantTypes.ClientCredentials,
            OpenIddictConstants.GrantTypes.RefreshToken,
            "custom_flow",
            "urn:custom:flow"
        ]);
        serverOptions.Scopes.Should().Contain([
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Email,
            "custom_scope1",
            "custom_scope2"
        ]);
        serverOptions.AccessTokenLifetime.Should().Be(TimeSpan.FromMinutes(120));
        serverOptions.RefreshTokenLifetime.Should().Be(TimeSpan.FromDays(60));
        serverOptions.AuthorizationCodeLifetime.Should().Be(TimeSpan.FromMinutes(15));
        serverOptions.IdentityTokenLifetime.Should().Be(TimeSpan.FromMinutes(30));
        serverOptions.DeviceCodeLifetime.Should().Be(TimeSpan.FromMinutes(20));
        serverOptions.RefreshTokenReuseLeeway.Should().Be(TimeSpan.FromSeconds(10));
        serverOptions.RequireProofKeyForCodeExchange.Should().BeTrue();
        serverOptions.DisableAccessTokenEncryption.Should().BeTrue();
        serverOptions.DisableRollingRefreshTokens.Should().BeTrue();
        serverOptions.DisableSlidingRefreshTokenExpiration.Should().BeTrue();
        serverOptions.UseReferenceAccessTokens.Should().BeTrue();
        serverOptions.UseReferenceRefreshTokens.Should().BeTrue();
        serverOptions.AcceptAnonymousClients.Should().BeTrue();
        serverOptions.UserCodeLength.Should().Be(8);
        serverOptions.UserCodeDisplayFormat.Should().Be("****-****");
        serverOptions.CodeChallengeMethods.Should().Contain(OpenIddictConstants.CodeChallengeMethods.Sha256);
    }

    [Fact]
    public void OnPostUpdateOidcSettings_WhenServerOptionsNull_FailsGracefully()
    {
        var model = new IndexModel(
            cleanupJobManager: null,
            dashboardOptions: null,
            serverOptions: null)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Substitute.For<ITempDataProvider>())
        };

        var result = model.OnPostUpdateOidcSettings(allowAuthCode: true);

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.RouteValues.Should().NotBeNull();
        redirect.RouteValues!["activeTab"].Should().Be("oidc");

        model.IsSuccess.Should().BeFalse();
        model.Message.Should().Contain("not registered");
    }

    [Fact]
    public async Task OnPostExportJsonAsync_WhenServiceAvailable_ReturnsFileResult()
    {
        var exportService = Substitute.For<IConfigurationExportImportService>();
        exportService.ExportConfigurationAsJsonAsync(Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("{\"version\":\"1.0\"}"));

        var model = new IndexModel(exportImportService: exportService)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Substitute.For<ITempDataProvider>())
        };

        var result = await model.OnPostExportJsonAsync();

        result.Should().BeOfType<FileContentResult>();
        var fileResult = (FileContentResult)result;
        fileResult.ContentType.Should().Be("application/json");
        fileResult.FileDownloadName.Should().StartWith("openiddict-configuration-").And.EndWith(".json");
        System.Text.Encoding.UTF8.GetString(fileResult.FileContents).Should().Be("{\"version\":\"1.0\"}");
    }

    [Fact]
    public async Task OnPostExportJsonAsync_WhenServiceUnavailable_RedirectsWithError()
    {
        var model = new IndexModel(exportImportService: null)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Substitute.For<ITempDataProvider>())
        };

        var result = await model.OnPostExportJsonAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.RouteValues!["activeTab"].Should().Be("export-import");
        model.IsSuccess.Should().BeFalse();
        model.Message.Should().Contain("not registered");
    }

    [Fact]
    public async Task OnPostImportJsonAsync_WithValidJsonText_ImportsAndRedirectsWithSuccess()
    {
        var exportService = Substitute.For<IConfigurationExportImportService>();
        exportService.ImportConfigurationFromJsonAsync(Arg.Any<string>(), Arg.Any<ImportOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Result<ImportResultDto>.Success(new ImportResultDto
            {
                ApplicationsCreated = 2,
                ScopesCreated = 1,
                OpenIddictServerConfigImported = true,
                ManagementConfigImported = true
            }));

        var model = new IndexModel(exportImportService: exportService)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Substitute.For<ITempDataProvider>())
        };

        var result = await model.OnPostImportJsonAsync(
            importFile: null,
            importJsonText: "{\"version\":\"1.0\"}",
            overwriteExisting: true,
            importApplications: true,
            importScopes: true,
            importConfigurations: true);

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.RouteValues!["activeTab"].Should().Be("export-import");
        model.IsSuccess.Should().BeTrue();
        model.Message.Should().Contain("Import completed").And.Contain("Apps: +2").And.Contain("Scopes: +1");
    }

    [Fact]
    public async Task OnPostImportJsonAsync_WithUploadedFile_ImportsAndRedirects()
    {
        var exportService = Substitute.For<IConfigurationExportImportService>();
        exportService.ImportConfigurationFromJsonAsync(Arg.Any<string>(), Arg.Any<ImportOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Result<ImportResultDto>.Success(new ImportResultDto
            {
                ApplicationsCreated = 1
            }));

        var model = new IndexModel(exportImportService: exportService)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Substitute.For<ITempDataProvider>())
        };

        var jsonBytes = System.Text.Encoding.UTF8.GetBytes("{\"version\":\"1.0\"}");
        var stream = new MemoryStream(jsonBytes);
        IFormFile formFile = new FormFile(stream, 0, jsonBytes.Length, "importFile", "backup.json")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/json"
        };

        var result = await model.OnPostImportJsonAsync(
            importFile: formFile,
            importJsonText: null);

        result.Should().BeOfType<RedirectToPageResult>();
        model.IsSuccess.Should().BeTrue();
        model.Message.Should().Contain("Import completed");
    }

    [Fact]
    public async Task OnPostImportJsonAsync_WhenNoInputProvided_RedirectsWithError()
    {
        var exportService = Substitute.For<IConfigurationExportImportService>();

        var model = new IndexModel(exportImportService: exportService)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Substitute.For<ITempDataProvider>())
        };

        var result = await model.OnPostImportJsonAsync(
            importFile: null,
            importJsonText: "   ");

        result.Should().BeOfType<RedirectToPageResult>();
        model.IsSuccess.Should().BeFalse();
        model.Message.Should().Contain("Please select a JSON file");
    }
}
