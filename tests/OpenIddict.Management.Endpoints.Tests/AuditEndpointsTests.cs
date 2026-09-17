using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Endpoints.Extensions;
using OpenIddict.Management.Models;
using OpenIddict.Management.Options;
using OpenIddict.Management.Services;
using Xunit;

namespace OpenIddict.Management.Endpoints.Tests;

public class AuditEndpointsTests
{
    [Fact]
    public async Task AuditEndpoints_WhenEnableAuditLoggingIsFalse_Returns403Forbidden()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddSingleton<IAuditTrailStore, InMemoryAuditTrailStore>();
        builder.Services.Configure<OpenIddictManagementOptions>(opts => opts.EnableAuditLogging = false);

        var app = builder.Build();
        app.MapOpenIddictManagementEndpoints(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/management/audit");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var detailResponse = await client.GetAsync("/api/management/audit/123");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AuditEndpoints_WhenEnableAuditLoggingIsTrue_Returns200OK()
    {
        var store = new InMemoryAuditTrailStore();
        var entry = new ManagementAuditEntry
        {
            Category = "Application",
            Action = "Created",
            EntityId = "app-100",
            EntityName = "Test App",
            Actor = "admin",
            Success = true
        };
        await store.RecordAsync(entry);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddSingleton<IAuditTrailStore>(store);
        builder.Services.Configure<OpenIddictManagementOptions>(opts => opts.EnableAuditLogging = true);

        var app = builder.Build();
        app.MapOpenIddictManagementEndpoints(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/management/audit");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<ManagementAuditEntry>>();
        pagedResult.Should().NotBeNull();
        pagedResult!.TotalCount.Should().Be(1);
        pagedResult.Items[0].EntityId.Should().Be("app-100");

        var detailResponse = await client.GetAsync($"/api/management/audit/{entry.Id}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await detailResponse.Content.ReadFromJsonAsync<ManagementAuditEntry>();
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(entry.Id);
    }
}
