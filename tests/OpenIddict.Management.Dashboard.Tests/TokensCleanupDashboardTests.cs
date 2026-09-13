using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dashboard.Pages.Tokens;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Results;
using Xunit;

namespace OpenIddict.Management.Dashboard.Tests;

public class TokensCleanupDashboardTests
{
    private static (IndexModel model, IOpenIddictRevocationManager revocationManager, ITokenCleanupJobManager cleanupJobManager) CreateModel()
    {
        var revocationManager = Substitute.For<IOpenIddictRevocationManager>();
        revocationManager.ListTokensAsync(Arg.Any<TokenFilterRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PagedResult<TokenListDto> { Items = [], PageIndex = 1, PageSize = 10, TotalCount = 0 }));
        revocationManager.GetTokenCountsAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success(new TokenCountSummaryDto()));
        revocationManager.ListSessionsAsync(Arg.Any<SessionFilterRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PagedResult<SessionListDto> { Items = [], PageIndex = 1, PageSize = 10, TotalCount = 0 }));
        revocationManager.GetTokenCountsByApplicationAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success(new List<ApplicationTokenCountDto>()));
        revocationManager.GetTokenTimelineAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new List<TokenTimelineDataPointDto>()));

        var cleanupJobManager = Substitute.For<ITokenCleanupJobManager>();
        cleanupJobManager.IsEnabled.Returns(true);
        cleanupJobManager.BatchSize.Returns(150);
        cleanupJobManager.Interval.Returns(TimeSpan.FromMinutes(30));
        cleanupJobManager.IncludeRevoked.Returns(true);
        cleanupJobManager.LastRunTime.Returns(DateTimeOffset.UtcNow);
        cleanupJobManager.LastPrunedCount.Returns(12);
        cleanupJobManager.TotalPrunedCount.Returns(45);
        cleanupJobManager.LastStatus.Returns("Idle");

        var model = new IndexModel(revocationManager, cleanupJobManager)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Substitute.For<ITempDataProvider>())
        };

        return (model, revocationManager, cleanupJobManager);
    }

    [Fact]
    public async Task OnGetAsync_PopulatesCleanupJobProperties()
    {
        var (model, _, _) = CreateModel();

        await model.OnGetAsync(CancellationToken.None);

        model.CleanupJobEnabled.Should().BeTrue();
        model.CleanupJobBatchSize.Should().Be(150);
        model.CleanupJobIntervalMinutes.Should().Be(30);
        model.CleanupJobIncludeRevoked.Should().BeTrue();
        model.CleanupJobLastRun.Should().NotBeNull();
        model.CleanupJobLastPrunedCount.Should().Be(12);
        model.CleanupJobTotalPrunedCount.Should().Be(45);
        model.CleanupJobStatus.Should().Be("Idle");
    }

    [Fact]
    public void OnPostToggleCleanupJob_TogglesStateAndSetsMessage()
    {
        var (model, _, cleanupJobManager) = CreateModel();

        var result = model.OnPostToggleCleanupJob();

        result.Should().BeOfType<RedirectToPageResult>();
        cleanupJobManager.Received(1).SetEnabled(false);
        model.IsSuccess.Should().BeTrue();
        model.Message.Should().Contain("paused");
    }

    [Fact]
    public void OnPostUpdateCleanupJobSettings_ValidAmount_UpdatesSettings()
    {
        var (model, _, cleanupJobManager) = CreateModel();

        var result = model.OnPostUpdateCleanupJobSettings(batchSize: 500, intervalMinutes: 15, includeRevoked: true);

        result.Should().BeOfType<RedirectToPageResult>();
        cleanupJobManager.Received(1).UpdateSettings(
            isEnabled: null,
            batchSize: 500,
            interval: TimeSpan.FromMinutes(15),
            includeRevoked: true);
        model.IsSuccess.Should().BeTrue();
        model.Message.Should().Contain("500");
    }

    [Fact]
    public void OnPostUpdateCleanupJobSettings_InvalidAmount_Fails()
    {
        var (model, _, cleanupJobManager) = CreateModel();

        var result = model.OnPostUpdateCleanupJobSettings(batchSize: 0);

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
    public async Task OnPostRunCleanupJobNowAsync_TriggersCleanupRun()
    {
        var (model, _, cleanupJobManager) = CreateModel();
        cleanupJobManager.TriggerRunAsync(Arg.Any<CancellationToken>())
            .Returns(27);

        var result = await model.OnPostRunCleanupJobNowAsync(CancellationToken.None);

        result.Should().BeOfType<RedirectToPageResult>();
        await cleanupJobManager.Received(1).TriggerRunAsync(Arg.Any<CancellationToken>());
        model.IsSuccess.Should().BeTrue();
        model.Message.Should().Contain("27");
    }
}
