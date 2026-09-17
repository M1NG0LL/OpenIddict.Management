using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Endpoints.Extensions;
using OpenIddict.Management.Endpoints.Health;
using OpenIddict.Management.Models;
using OpenIddict.Management.Results;
using Xunit;

namespace OpenIddict.Management.Endpoints.Tests;

public class BulkAndConfigurationEndpointsTests
{
    [Fact]
    public async Task BulkEndpoints_ApplicationAndScope_ReturnExpectedResponses()
    {
        var appService = Substitute.For<IApplicationManagementService>();
        var scopeService = Substitute.For<IScopeManagementService>();

        appService.BulkCreateAsync(Arg.Any<IEnumerable<ApplicationCreateDto>>(), Arg.Any<CancellationToken>())
            .Returns(Result<BulkOperationResultDto>.Success(new BulkOperationResultDto { SuccessCount = 2 }));

        appService.BulkDeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result<BulkOperationResultDto>.Success(new BulkOperationResultDto { SuccessCount = 2 }));

        scopeService.BulkCreateAsync(Arg.Any<IEnumerable<CreateScopeRequest>>(), Arg.Any<CancellationToken>())
            .Returns(Result<BulkOperationResultDto>.Success(new BulkOperationResultDto { SuccessCount = 1 }));

        scopeService.BulkDeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result<BulkOperationResultDto>.Success(new BulkOperationResultDto { SuccessCount = 1 }));

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddSingleton(appService);
        builder.Services.AddSingleton(scopeService);

        var app = builder.Build();
        app.MapOpenIddictManagementEndpoints(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        // 1. Applications bulk create
        var appCreateResp = await client.PostAsJsonAsync("/api/management/applications/bulk-create", new List<ApplicationCreateDto>
        {
            new() { ClientId = "app1", DisplayName = "App 1" },
            new() { ClientId = "app2", DisplayName = "App 2" }
        });
        appCreateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var appCreateResult = await appCreateResp.Content.ReadFromJsonAsync<BulkOperationResultDto>();
        appCreateResult!.SuccessCount.Should().Be(2);

        // 2. Applications bulk delete
        var appDeleteResp = await client.PostAsJsonAsync("/api/management/applications/bulk-delete", new List<string> { "app1", "app2" });
        appDeleteResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var appDeleteResult = await appDeleteResp.Content.ReadFromJsonAsync<BulkOperationResultDto>();
        appDeleteResult!.SuccessCount.Should().Be(2);

        // 3. Scopes bulk create
        var scopeCreateResp = await client.PostAsJsonAsync("/api/management/scopes/bulk-create", new List<CreateScopeRequest>
        {
            new() { Name = "scope1" }
        });
        scopeCreateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var scopeCreateResult = await scopeCreateResp.Content.ReadFromJsonAsync<BulkOperationResultDto>();
        scopeCreateResult!.SuccessCount.Should().Be(1);

        // 4. Scopes bulk delete
        var scopeDeleteResp = await client.PostAsJsonAsync("/api/management/scopes/bulk-delete", new List<string> { "scope1" });
        scopeDeleteResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var scopeDeleteResult = await scopeDeleteResp.Content.ReadFromJsonAsync<BulkOperationResultDto>();
        scopeDeleteResult!.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task ConfigurationEndpoints_ExportAndImport_ReturnExpectedResponses()
    {
        var configService = Substitute.For<IConfigurationExportImportService>();
        var package = new ManagementExportPackage
        {
            Applications = [new ApplicationExportDto { ClientId = "app-export" }],
            Scopes = [new ScopeExportDto { Name = "scope-export" }]
        };

        configService.ExportConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(Result<ManagementExportPackage>.Success(package));

        configService.ImportConfigurationAsync(Arg.Any<ManagementExportPackage>(), Arg.Any<ImportOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Result<ImportResultDto>.Success(new ImportResultDto { ApplicationsCreated = 1, ScopesCreated = 1 }));

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddSingleton(configService);

        var app = builder.Build();
        app.MapOpenIddictManagementEndpoints(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        // Export
        var exportResp = await client.GetAsync("/api/management/configuration/export");
        exportResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var exported = await exportResp.Content.ReadFromJsonAsync<ManagementExportPackage>();
        exported.Should().NotBeNull();
        exported!.Applications.Should().ContainSingle();

        // Import
        var importResp = await client.PostAsJsonAsync("/api/management/configuration/import", package);
        importResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var imported = await importResp.Content.ReadFromJsonAsync<ImportResultDto>();
        imported.Should().NotBeNull();
        imported!.ApplicationsCreated.Should().Be(1);
    }

    [Fact]
    public async Task ConfigurationEndpoints_ImportWithConfigurationsOption_PassesOptionToService()
    {
        var configService = Substitute.For<IConfigurationExportImportService>();
        var package = new ManagementExportPackage
        {
            OpenIddictServer = new OpenIddictServerConfigurationDto
            {
                Issuer = "https://server.example.com"
            },
            Management = new OpenIddictManagementConfigurationDto
            {
                RoutePrefix = "/mgmt"
            }
        };

        ImportOptions? capturedOptions = null;
        configService.ImportConfigurationAsync(
            Arg.Any<ManagementExportPackage>(),
            Arg.Do<ImportOptions?>(opts => capturedOptions = opts),
            Arg.Any<CancellationToken>())
            .Returns(Result<ImportResultDto>.Success(new ImportResultDto
            {
                OpenIddictServerConfigImported = true,
                ManagementConfigImported = true
            }));

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddSingleton(configService);

        var app = builder.Build();
        app.MapOpenIddictManagementEndpoints(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        var importResp = await client.PostAsJsonAsync("/api/management/configuration/import?importConfigurations=false", package);
        importResp.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedOptions.Should().NotBeNull();
        capturedOptions!.ImportConfigurations.Should().BeFalse();
    }

    [Fact]
    public async Task TokenCleanupHealthCheck_ReportsExpectedStatus()
    {
        var jobManager = Substitute.For<ITokenCleanupJobManager>();
        jobManager.IsEnabled.Returns(true);
        jobManager.LastStatus.Returns("Idle");
        jobManager.LastError.Returns((string?)null);

        var healthCheck = new TokenCleanupHealthCheck(jobManager);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["isEnabled"].Should().Be(true);
        result.Data["status"].Should().Be("Idle");

        // When job had error
        jobManager.LastError.Returns("Database connection timeout");
        var degradedResult = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        degradedResult.Status.Should().Be(HealthStatus.Degraded);
        degradedResult.Description.Should().Contain("Database connection timeout");
    }
}
