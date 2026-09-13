using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dashboard.Pages.Settings;
using OpenIddict.Management.Options;
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
    public void OnGet_PopulatesTokensTabPropertiesAndGeneralSettings()
    {
        var (model, _) = CreateModel();

        model.OnGet();

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
}
