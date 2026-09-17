using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Models;
using OpenIddict.Management.Options;
using OpenIddict.Management.Results;
using OpenIddict.Management.Services;
using OpenIddict.Server;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class ConfigurationExportImportServiceTests
{
    [Fact]
    public async Task ExportConfigurationAsync_GathersApplicationsAndScopes()
    {
        var appService = Substitute.For<IApplicationManagementService>();
        var scopeService = Substitute.For<IScopeManagementService>();

        var apps = new List<ApplicationListDto>
        {
            new() { Id = "1", ClientId = "app-1", DisplayName = "App 1", ClientType = "confidential", CreatedAt = DateTimeOffset.UtcNow }
        };
        var appDetails = new ManagedApplication
        {
            Id = "1",
            ClientId = "app-1",
            DisplayName = "App 1",
            ClientType = "confidential",
            CreatedAt = DateTimeOffset.UtcNow,
            RedirectUris = ["https://localhost/callback"],
            Permissions = ["ept:authorization"]
        };

        appService.ListAsync(Arg.Any<PagedRequest>(), Arg.Any<Enums.ApplicationStatus?>(), Arg.Any<Enums.ApplicationEnvironment?>(), Arg.Any<List<string>?>(), Arg.Any<CancellationToken>())
            .Returns(Result<PagedResult<ApplicationListDto>>.Success(new PagedResult<ApplicationListDto> { Items = apps, TotalCount = 1, PageIndex = 1, PageSize = 10 }));
        appService.GetByIdAsync("1", Arg.Any<CancellationToken>())
            .Returns(Result<ManagedApplication>.Success(appDetails));

        var scopes = new List<ManagedScope>
        {
            new() { Id = "s1", Name = "api_scope", DisplayName = "API Scope", CreatedAt = DateTimeOffset.UtcNow, Resources = ["res1"] }
        };
        scopeService.ListAsync(Arg.Any<PagedRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<PagedResult<ManagedScope>>.Success(new PagedResult<ManagedScope> { Items = scopes, TotalCount = 1, PageIndex = 1, PageSize = 10 }));

        var exportService = new ConfigurationExportImportService(appService, scopeService);

        var result = await exportService.ExportConfigurationAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Applications.Should().ContainSingle();
        result.Value.Applications[0].ClientId.Should().Be("app-1");
        result.Value.Applications[0].ClientType.Should().Be("confidential");
        result.Value.Scopes.Should().ContainSingle();
        result.Value.Scopes[0].Name.Should().Be("api_scope");
    }

    [Fact]
    public async Task ImportConfigurationAsync_ImportsNewItems()
    {
        var appService = Substitute.For<IApplicationManagementService>();
        var scopeService = Substitute.For<IScopeManagementService>();

        // Application does not exist -> Create succeeds
        appService.GetByClientIdAsync("new-app", Arg.Any<CancellationToken>())
            .Returns(Result<ManagedApplication>.Failure(ManagementError.EntityNotFound("Application", "new-app")));
        appService.CreateAsync(Arg.Any<ApplicationCreateDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<ManagedApplication>.Success(new ManagedApplication { Id = "1", ClientId = "new-app", CreatedAt = DateTimeOffset.UtcNow }));

        // Scope does not exist -> Create succeeds
        scopeService.GetByNameAsync("new-scope", Arg.Any<CancellationToken>())
            .Returns(Result<ManagedScope>.Failure(ManagementError.EntityNotFound("Scope", "new-scope")));
        scopeService.CreateAsync(Arg.Any<CreateScopeRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<ManagedScope>.Success(new ManagedScope { Id = "s1", Name = "new-scope", CreatedAt = DateTimeOffset.UtcNow }));

        var package = new ManagementExportPackage
        {
            Applications = [new ApplicationExportDto { ClientId = "new-app", DisplayName = "New App" }],
            Scopes = [new ScopeExportDto { Name = "new-scope", DisplayName = "New Scope" }]
        };

        var service = new ConfigurationExportImportService(appService, scopeService);
        var result = await service.ImportConfigurationAsync(package);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ApplicationsCreated.Should().Be(1);
        result.Value.ScopesCreated.Should().Be(1);
        result.Value.ApplicationsUpdated.Should().Be(0);
        result.Value.ScopesUpdated.Should().Be(0);
    }

    [Fact]
    public async Task ExportConfigurationAsync_WithServerAndManagementOptions_ExportsAllConfigurations()
    {
        var appService = Substitute.For<IApplicationManagementService>();
        var scopeService = Substitute.For<IScopeManagementService>();

        appService.ListAsync(Arg.Any<PagedRequest>(), Arg.Any<Enums.ApplicationStatus?>(), Arg.Any<Enums.ApplicationEnvironment?>(), Arg.Any<List<string>?>(), Arg.Any<CancellationToken>())
            .Returns(Result<PagedResult<ApplicationListDto>>.Success(new PagedResult<ApplicationListDto> { Items = [], TotalCount = 0, PageIndex = 1, PageSize = 10 }));
        scopeService.ListAsync(Arg.Any<PagedRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<PagedResult<ManagedScope>>.Success(new PagedResult<ManagedScope> { Items = [], TotalCount = 0, PageIndex = 1, PageSize = 10 }));

        var serverOptions = new OpenIddictServerOptions
        {
            Issuer = new Uri("https://auth.example.com/"),
            AccessTokenLifetime = TimeSpan.FromMinutes(45),
            RefreshTokenLifetime = TimeSpan.FromDays(30),
            AuthorizationCodeLifetime = TimeSpan.FromMinutes(10),
            RequireProofKeyForCodeExchange = true,
            DisableAccessTokenEncryption = true,
            AcceptAnonymousClients = true,
            EnableDegradedMode = false,
            UserCodeLength = 8,
            UserCodeDisplayFormat = "####-####"
        };
        serverOptions.AuthorizationEndpointUris.Add(new Uri("/connect/authorize", UriKind.Relative));
        serverOptions.TokenEndpointUris.Add(new Uri("/connect/token", UriKind.Relative));
        serverOptions.GrantTypes.Add("authorization_code");
        serverOptions.GrantTypes.Add("refresh_token");
        serverOptions.ResponseTypes.Add("code");
        serverOptions.Scopes.Clear();
        serverOptions.Scopes.Add("api");
        serverOptions.Claims.Clear();
        serverOptions.Claims.Add("sub");
        serverOptions.CodeChallengeMethods.Add("S256");

        var managementOptions = new OpenIddictManagementOptions
        {
            RoutePrefix = "/custom/management",
            RequireHttps = false,
            EnableAuditLogging = true
        };

        var cleanupJobManager = Substitute.For<ITokenCleanupJobManager>();
        cleanupJobManager.IsEnabled.Returns(true);
        cleanupJobManager.BatchSize.Returns(250);
        cleanupJobManager.Interval.Returns(TimeSpan.FromHours(12));
        cleanupJobManager.IncludeRevoked.Returns(false);

        var service = new ConfigurationExportImportService(
            appService,
            scopeService,
            managementOptions: Microsoft.Extensions.Options.Options.Create(managementOptions),
            cleanupJobManager: cleanupJobManager,
            serverOptions: Microsoft.Extensions.Options.Options.Create(serverOptions));

        var result = await service.ExportConfigurationAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        // Verify Server Options Export
        result.Value!.OpenIddictServer.Should().NotBeNull();
        var sDto = result.Value.OpenIddictServer!;
        sDto.Issuer.Should().Be("https://auth.example.com/");
        sDto.AuthorizationEndpointUris.Should().ContainSingle().Which.Should().Be("/connect/authorize");
        sDto.TokenEndpointUris.Should().ContainSingle().Which.Should().Be("/connect/token");
        sDto.AccessTokenLifetime.Should().Be(TimeSpan.FromMinutes(45));
        sDto.RefreshTokenLifetime.Should().Be(TimeSpan.FromDays(30));
        sDto.AuthorizationCodeLifetime.Should().Be(TimeSpan.FromMinutes(10));
        sDto.GrantTypes.Should().BeEquivalentTo(["authorization_code", "refresh_token"]);
        sDto.ResponseTypes.Should().BeEquivalentTo(["code"]);
        sDto.Scopes.Should().BeEquivalentTo(["api"]);
        sDto.Claims.Should().BeEquivalentTo(["sub"]);
        sDto.CodeChallengeMethods.Should().BeEquivalentTo(["S256"]);
        sDto.RequireProofKeyForCodeExchange.Should().BeTrue();
        sDto.DisableAccessTokenEncryption.Should().BeTrue();
        sDto.AcceptAnonymousClients.Should().BeTrue();
        sDto.UserCodeLength.Should().Be(8);
        sDto.UserCodeDisplayFormat.Should().Be("####-####");

        // Verify Management Options Export
        result.Value.Management.Should().NotBeNull();
        var mDto = result.Value.Management!;
        mDto.RoutePrefix.Should().Be("/custom/management");
        mDto.RequireHttps.Should().BeFalse();
        mDto.EnableAuditLogging.Should().BeTrue();
        mDto.TokenCleanup.Should().NotBeNull();
        mDto.TokenCleanup!.IsEnabled.Should().BeTrue();
        mDto.TokenCleanup.BatchSize.Should().Be(250);
        mDto.TokenCleanup.Interval.Should().Be(TimeSpan.FromHours(12));
        mDto.TokenCleanup.IncludeRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task ImportConfigurationAsync_WithServerAndManagementConfigurations_AppliesConfigurations()
    {
        var appService = Substitute.For<IApplicationManagementService>();
        var scopeService = Substitute.For<IScopeManagementService>();

        var serverOptions = new OpenIddictServerOptions();
        var managementOptions = new OpenIddictManagementOptions();
        var cleanupJobManager = Substitute.For<ITokenCleanupJobManager>();

        var package = new ManagementExportPackage
        {
            OpenIddictServer = new OpenIddictServerConfigurationDto
            {
                Issuer = "https://imported.example.com/",
                AuthorizationEndpointUris = ["/oauth/authorize"],
                TokenEndpointUris = ["/oauth/token"],
                LogoutEndpointUris = ["/oauth/logout"],
                AccessTokenLifetime = TimeSpan.FromHours(2),
                RefreshTokenLifetime = TimeSpan.FromDays(60),
                GrantTypes = ["authorization_code", "client_credentials"],
                ResponseTypes = ["code"],
                Scopes = ["openid", "profile"],
                Claims = ["sub", "email"],
                CodeChallengeMethods = ["S256"],
                RequireProofKeyForCodeExchange = true,
                DisableAccessTokenEncryption = false,
                UseReferenceAccessTokens = true,
                UserCodeLength = 6
            },
            Management = new OpenIddictManagementConfigurationDto
            {
                RoutePrefix = "/api/admin",
                RequireHttps = true,
                EnableAuditLogging = true,
                TokenCleanup = new TokenCleanupConfigurationDto
                {
                    IsEnabled = true,
                    BatchSize = 500,
                    Interval = TimeSpan.FromHours(6),
                    IncludeRevoked = true
                }
            }
        };

        var service = new ConfigurationExportImportService(
            appService,
            scopeService,
            managementOptions: Microsoft.Extensions.Options.Options.Create(managementOptions),
            cleanupJobManager: cleanupJobManager,
            serverOptions: Microsoft.Extensions.Options.Options.Create(serverOptions));

        var result = await service.ImportConfigurationAsync(package);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.OpenIddictServerConfigImported.Should().BeTrue();
        result.Value.ManagementConfigImported.Should().BeTrue();

        // Verify OpenIddictServerOptions applied
        serverOptions.Issuer.Should().Be(new Uri("https://imported.example.com/"));
        serverOptions.AuthorizationEndpointUris.Select(u => u.OriginalString).Should().BeEquivalentTo(["/oauth/authorize"]);
        serverOptions.TokenEndpointUris.Select(u => u.OriginalString).Should().BeEquivalentTo(["/oauth/token"]);
        serverOptions.LogoutEndpointUris.Select(u => u.OriginalString).Should().BeEquivalentTo(["/oauth/logout"]);
        serverOptions.AccessTokenLifetime.Should().Be(TimeSpan.FromHours(2));
        serverOptions.RefreshTokenLifetime.Should().Be(TimeSpan.FromDays(60));
        serverOptions.GrantTypes.Should().BeEquivalentTo(["authorization_code", "client_credentials"]);
        serverOptions.ResponseTypes.Should().BeEquivalentTo(["code"]);
        serverOptions.Scopes.Should().BeEquivalentTo(["openid", "profile"]);
        serverOptions.Claims.Should().BeEquivalentTo(["sub", "email"]);
        serverOptions.CodeChallengeMethods.Should().BeEquivalentTo(["S256"]);
        serverOptions.RequireProofKeyForCodeExchange.Should().BeTrue();
        serverOptions.UseReferenceAccessTokens.Should().BeTrue();
        serverOptions.UserCodeLength.Should().Be(6);

        // Verify OpenIddictManagementOptions applied
        managementOptions.RoutePrefix.Should().Be("/api/admin");
        managementOptions.RequireHttps.Should().BeTrue();
        managementOptions.EnableAuditLogging.Should().BeTrue();

        // Verify Cleanup Job Manager settings updated
        cleanupJobManager.Received(1).UpdateSettings(true, 500, TimeSpan.FromHours(6), true);
    }

    [Fact]
    public async Task ImportConfigurationAsync_WhenImportConfigurationsIsFalse_DoesNotApplyConfigurations()
    {
        var appService = Substitute.For<IApplicationManagementService>();
        var scopeService = Substitute.For<IScopeManagementService>();

        var serverOptions = new OpenIddictServerOptions
        {
            Issuer = new Uri("https://initial.example.com/")
        };
        var managementOptions = new OpenIddictManagementOptions
        {
            RoutePrefix = "/initial"
        };
        var cleanupJobManager = Substitute.For<ITokenCleanupJobManager>();

        var package = new ManagementExportPackage
        {
            OpenIddictServer = new OpenIddictServerConfigurationDto
            {
                Issuer = "https://changed.example.com/"
            },
            Management = new OpenIddictManagementConfigurationDto
            {
                RoutePrefix = "/changed"
            }
        };

        var service = new ConfigurationExportImportService(
            appService,
            scopeService,
            managementOptions: Microsoft.Extensions.Options.Options.Create(managementOptions),
            cleanupJobManager: cleanupJobManager,
            serverOptions: Microsoft.Extensions.Options.Options.Create(serverOptions));

        var result = await service.ImportConfigurationAsync(package, new ImportOptions { ImportConfigurations = false });

        result.IsSuccess.Should().BeTrue();
        result.Value!.OpenIddictServerConfigImported.Should().BeFalse();
        result.Value.ManagementConfigImported.Should().BeFalse();

        // Assert no changes applied
        serverOptions.Issuer.Should().Be(new Uri("https://initial.example.com/"));
        managementOptions.RoutePrefix.Should().Be("/initial");
        cleanupJobManager.DidNotReceiveWithAnyArgs().UpdateSettings(default, default, default, default);
    }

    [Fact]
    public async Task JsonExportAndImport_RoundtripsSuccessfully()
    {
        var appService = Substitute.For<IApplicationManagementService>();
        var scopeService = Substitute.For<IScopeManagementService>();

        appService.ListAsync(Arg.Any<PagedRequest>(), Arg.Any<Enums.ApplicationStatus?>(), Arg.Any<Enums.ApplicationEnvironment?>(), Arg.Any<List<string>?>(), Arg.Any<CancellationToken>())
            .Returns(Result<PagedResult<ApplicationListDto>>.Success(new PagedResult<ApplicationListDto> { Items = [], TotalCount = 0, PageIndex = 1, PageSize = 10 }));
        scopeService.ListAsync(Arg.Any<PagedRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<PagedResult<ManagedScope>>.Success(new PagedResult<ManagedScope> { Items = [], TotalCount = 0, PageIndex = 1, PageSize = 10 }));

        var originalServer = new OpenIddictServerOptions
        {
            Issuer = new Uri("https://auth.example.com/"),
            AccessTokenLifetime = TimeSpan.FromHours(1),
            RequireProofKeyForCodeExchange = true
        };
        originalServer.GrantTypes.Add("authorization_code");

        var originalManagement = new OpenIddictManagementOptions
        {
            RoutePrefix = "/api/v1/mgmt",
            RequireHttps = true,
            EnableAuditLogging = true
        };

        var service = new ConfigurationExportImportService(
            appService,
            scopeService,
            managementOptions: Microsoft.Extensions.Options.Options.Create(originalManagement),
            serverOptions: Microsoft.Extensions.Options.Options.Create(originalServer));

        // 1. Export as JSON
        var jsonResult = await service.ExportConfigurationAsJsonAsync();
        jsonResult.IsSuccess.Should().BeTrue();
        jsonResult.Value.Should().NotBeNullOrWhiteSpace();

        // 2. Import into target options
        var targetServer = new OpenIddictServerOptions();
        var targetManagement = new OpenIddictManagementOptions();

        var importService = new ConfigurationExportImportService(
            appService,
            scopeService,
            managementOptions: Microsoft.Extensions.Options.Options.Create(targetManagement),
            serverOptions: Microsoft.Extensions.Options.Options.Create(targetServer));

        var importResult = await importService.ImportConfigurationFromJsonAsync(jsonResult.Value!);
        importResult.IsSuccess.Should().BeTrue();
        importResult.Value!.OpenIddictServerConfigImported.Should().BeTrue();
        importResult.Value.ManagementConfigImported.Should().BeTrue();

        targetServer.Issuer.Should().Be(new Uri("https://auth.example.com/"));
        targetServer.AccessTokenLifetime.Should().Be(TimeSpan.FromHours(1));
        targetServer.RequireProofKeyForCodeExchange.Should().BeTrue();
        targetServer.GrantTypes.Should().Contain("authorization_code");
        targetManagement.RoutePrefix.Should().Be("/api/v1/mgmt");
    }
}
