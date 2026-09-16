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
    private static (IndexModel model, IOpenIddictTokenManager tokenManager, IOpenIddictAuthorizationManager authorizationManager, ITokenCleanupJobManager cleanupJobManager) CreateModel()
    {
        var tokenManager = Substitute.For<IOpenIddictTokenManager>();
        tokenManager.ListTokensAsync(Arg.Any<TokenFilterRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PagedResult<TokenListDto> { Items = [], PageIndex = 1, PageSize = 10, TotalCount = 0 }));
        tokenManager.GetTokenCountsAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success(new TokenCountSummaryDto()));
        tokenManager.GetTokenCountsByApplicationAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success(new List<ApplicationTokenCountDto>()));
        tokenManager.GetTokenTimelineAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new List<TokenTimelineDataPointDto>()));

        var authorizationManager = Substitute.For<IOpenIddictAuthorizationManager>();
        authorizationManager.ListSessionsAsync(Arg.Any<SessionFilterRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PagedResult<SessionListDto> { Items = [], PageIndex = 1, PageSize = 10, TotalCount = 0 }));

        var cleanupJobManager = Substitute.For<ITokenCleanupJobManager>();
        cleanupJobManager.IsEnabled.Returns(true);
        cleanupJobManager.BatchSize.Returns(150);
        cleanupJobManager.Interval.Returns(TimeSpan.FromMinutes(30));
        cleanupJobManager.IncludeRevoked.Returns(true);
        cleanupJobManager.LastRunTime.Returns(DateTimeOffset.UtcNow);
        cleanupJobManager.LastPrunedCount.Returns(12);
        cleanupJobManager.TotalPrunedCount.Returns(45);
        cleanupJobManager.LastStatus.Returns("Idle");

        var model = new IndexModel(tokenManager, authorizationManager, cleanupJobManager)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Substitute.For<ITempDataProvider>())
        };

        return (model, tokenManager, authorizationManager, cleanupJobManager);
    }

    [Fact]
    public async Task OnGetAsync_PopulatesCleanupJobProperties()
    {
        var (model, _, _, _) = CreateModel();

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
        var (model, _, _, cleanupJobManager) = CreateModel();

        var result = model.OnPostToggleCleanupJob();

        result.Should().BeOfType<RedirectToPageResult>();
        cleanupJobManager.Received(1).SetEnabled(false);
        model.IsSuccess.Should().BeTrue();
        model.Message.Should().Contain("paused");
    }

    [Fact]
    public void OnPostUpdateCleanupJobSettings_ValidAmount_UpdatesSettings()
    {
        var (model, _, _, cleanupJobManager) = CreateModel();

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
        var (model, _, _, cleanupJobManager) = CreateModel();

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
        var (model, _, _, cleanupJobManager) = CreateModel();
        cleanupJobManager.TriggerRunAsync(Arg.Any<CancellationToken>())
            .Returns(27);

        var result = await model.OnPostRunCleanupJobNowAsync(CancellationToken.None);

        result.Should().BeOfType<RedirectToPageResult>();
        await cleanupJobManager.Received(1).TriggerRunAsync(Arg.Any<CancellationToken>());
        model.IsSuccess.Should().BeTrue();
        model.Message.Should().Contain("27");
    }

    [Fact]
    public async Task OnPostExtendTokenExpirationAsync_ValidCall_CallsManagerAndSucceeds()
    {
        var (model, tokenManager, _, _) = CreateModel();
        tokenManager.ExtendTokenExpirationAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(1));

        var result = await model.OnPostExtendTokenExpirationAsync("tok-123", null, 45, CancellationToken.None);

        result.Should().BeOfType<RedirectToPageResult>();
        model.IsSuccess.Should().BeTrue();
        model.Message.Should().Contain("45");
        await tokenManager.Received(1).ExtendTokenExpirationAsync(
            Arg.Is<IReadOnlyList<string>>(l => l.Contains("tok-123")),
            45,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnPostRevokeSelectedTokensAsync_ValidTokens_RevokesAllSelected()
    {
        var (model, tokenManager, _, _) = CreateModel();
        tokenManager.RevokeMultipleTokensAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new RevocationResultDto { TokensRevoked = 3, Scope = OpenIddict.Management.Enums.RevocationScope.Token }));

        var result = await model.OnPostRevokeSelectedTokensAsync(["id1", "id2", "id3"], CancellationToken.None);

        result.Should().BeOfType<RedirectToPageResult>();
        model.IsSuccess.Should().BeTrue();
        model.Message.Should().Contain("3");
        await tokenManager.Received(1).RevokeMultipleTokensAsync(
            Arg.Is<IReadOnlyList<string>>(l => l.Count == 3),
            Arg.Any<CancellationToken>());
    }
}
