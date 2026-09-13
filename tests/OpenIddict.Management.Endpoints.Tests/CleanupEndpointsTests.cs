using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Endpoints.Extensions;
using Xunit;

namespace OpenIddict.Management.Endpoints.Tests;

public class CleanupEndpointsTests
{
    [Fact]
    public async Task CleanupEndpoints_WhenJobManagerNotRegistered_ReturnsServiceUnavailable()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();

        var app = builder.Build();
        app.MapOpenIddictManagementEndpoints(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/management/cleanup");
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        var toggleResponse = await client.PostAsync("/api/management/cleanup/toggle", null);
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        var settingsResponse = await client.PutAsJsonAsync("/api/management/cleanup/settings", new UpdateCleanupJobSettingsRequest { BatchSize = 50 });
        settingsResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        var runResponse = await client.PostAsync("/api/management/cleanup/run", null);
        runResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        await app.StopAsync();
    }

    [Fact]
    public async Task CleanupEndpoints_WhenJobManagerRegistered_ExecutesSuccessfully()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();

        var jobManager = Substitute.For<ITokenCleanupJobManager>();
        jobManager.IsEnabled.Returns(true);
        jobManager.BatchSize.Returns(100);
        jobManager.Interval.Returns(TimeSpan.FromHours(1));
        jobManager.IncludeRevoked.Returns(true);
        jobManager.LastStatus.Returns("Idle");
        jobManager.TriggerRunAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(5));

        builder.Services.AddSingleton(jobManager);

        var app = builder.Build();
        app.MapOpenIddictManagementEndpoints(opts => opts.RequireAuthorization = false);

        await app.StartAsync();
        var client = app.GetTestClient();

        // 1. GET /cleanup
        var statusResponse = await client.GetAsync("/api/management/cleanup");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await statusResponse.Content.ReadFromJsonAsync<CleanupJobStatusDto>();
        status.Should().NotBeNull();
        status!.IsEnabled.Should().BeTrue();
        status.BatchSize.Should().Be(100);
        status.IntervalMinutes.Should().Be(60);

        // 2. POST /cleanup/toggle
        var toggleResponse = await client.PostAsync("/api/management/cleanup/toggle?enabled=false", null);
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        jobManager.Received(1).SetEnabled(false);

        // 3. PUT /cleanup/settings
        var updateRequest = new UpdateCleanupJobSettingsRequest
        {
            BatchSize = 250,
            IntervalMinutes = 30,
            IncludeRevoked = false
        };
        var settingsResponse = await client.PutAsJsonAsync("/api/management/cleanup/settings", updateRequest);
        settingsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        jobManager.Received(1).UpdateSettings(
            batchSize: 250,
            interval: TimeSpan.FromMinutes(30),
            includeRevoked: false);

        // 4. POST /cleanup/run
        var runResponse = await client.PostAsync("/api/management/cleanup/run", null);
        runResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var runContent = await runResponse.Content.ReadAsStringAsync();
        runContent.Should().Contain("5");
        await jobManager.Received(1).TriggerRunAsync(Arg.Any<CancellationToken>());

        await app.StopAsync();
    }
}
