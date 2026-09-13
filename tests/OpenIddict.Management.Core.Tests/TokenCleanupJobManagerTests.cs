using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Options;
using OpenIddict.Management.Results;
using OpenIddict.Management.Services;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class TokenCleanupJobManagerTests
{
    [Fact]
    public void Constructor_WithDefaultOptions_InitializesCorrectly()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var manager = new TokenCleanupJobManager(scopeFactory);

        manager.IsEnabled.Should().BeFalse();
        manager.BatchSize.Should().Be(100);
        manager.Interval.Should().Be(TimeSpan.FromHours(24));
        manager.IncludeRevoked.Should().BeTrue();
        manager.LastRunTime.Should().BeNull();
        manager.LastPrunedCount.Should().Be(0);
        manager.TotalPrunedCount.Should().Be(0);
        manager.LastStatus.Should().Be("Paused");
        manager.LastError.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithCustomOptions_InitializesFromOptions()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var options = Microsoft.Extensions.Options.Options.Create(new TokenCleanupOptions
        {
            IsEnabled = false,
            BatchSize = 250,
            Interval = TimeSpan.FromMinutes(30),
            IncludeRevoked = false
        });

        var manager = new TokenCleanupJobManager(scopeFactory, options);

        manager.IsEnabled.Should().BeFalse();
        manager.BatchSize.Should().Be(250);
        manager.Interval.Should().Be(TimeSpan.FromMinutes(30));
        manager.IncludeRevoked.Should().BeFalse();
        manager.LastStatus.Should().Be("Paused");
    }

    [Fact]
    public void SetEnabled_TogglesWorkingState()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var manager = new TokenCleanupJobManager(scopeFactory);

        manager.SetEnabled(false);
        manager.IsEnabled.Should().BeFalse();
        manager.LastStatus.Should().Be("Paused");

        manager.SetEnabled(true);
        manager.IsEnabled.Should().BeTrue();
        manager.LastStatus.Should().Be("Idle");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(500)]
    public void SetBatchSize_ValidValue_UpdatesBatchSize(int batchSize)
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var manager = new TokenCleanupJobManager(scopeFactory);

        manager.SetBatchSize(batchSize);
        manager.BatchSize.Should().Be(batchSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void SetBatchSize_InvalidValue_ThrowsException(int invalidBatchSize)
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var manager = new TokenCleanupJobManager(scopeFactory);

        var act = () => manager.SetBatchSize(invalidBatchSize);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateSettings_UpdatesParameters()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var manager = new TokenCleanupJobManager(scopeFactory);

        manager.UpdateSettings(
            isEnabled: false,
            batchSize: 75,
            interval: TimeSpan.FromMinutes(15),
            includeRevoked: false);

        manager.IsEnabled.Should().BeFalse();
        manager.BatchSize.Should().Be(75);
        manager.Interval.Should().Be(TimeSpan.FromMinutes(15));
        manager.IncludeRevoked.Should().BeFalse();
        manager.LastStatus.Should().Be("Paused");
    }

    [Fact]
    public async Task RunJobCycleAsync_CallsRevocationManagerAndUpdatesStats()
    {
        var revocationManager = Substitute.For<IOpenIddictRevocationManager>();
        revocationManager.PruneTokensAsync(100, true, Arg.Any<CancellationToken>())
            .Returns(Result.Success(42));

        var services = new ServiceCollection();
        services.AddScoped(_ => revocationManager);
        var serviceProvider = services.BuildServiceProvider();

        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var options = Microsoft.Extensions.Options.Options.Create(new TokenCleanupOptions { IsEnabled = true });
        var manager = new TokenCleanupJobManager(scopeFactory, options);

        var prunedCount = await manager.RunJobCycleAsync(serviceProvider);

        prunedCount.Should().Be(42);
        manager.LastPrunedCount.Should().Be(42);
        manager.TotalPrunedCount.Should().Be(42);
        manager.LastRunTime.Should().NotBeNull();
        manager.LastStatus.Should().Be("Idle");
        manager.LastError.Should().BeNull();

        await revocationManager.Received(1).PruneTokensAsync(100, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunJobCycleAsync_WhenPruningFails_UpdatesStatusAndError()
    {
        var revocationManager = Substitute.For<IOpenIddictRevocationManager>();
        revocationManager.PruneTokensAsync(100, true, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<int>("PruneError", "Database timeout occurred."));

        var services = new ServiceCollection();
        services.AddScoped(_ => revocationManager);
        var serviceProvider = services.BuildServiceProvider();

        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var manager = new TokenCleanupJobManager(scopeFactory);

        var prunedCount = await manager.RunJobCycleAsync(serviceProvider);

        prunedCount.Should().Be(0);
        manager.LastStatus.Should().Be("Failed");
        manager.LastError.Should().Be("Database timeout occurred.");
    }

    [Fact]
    public async Task TriggerRunAsync_ExecutesSynchronously()
    {
        var revocationManager = Substitute.For<IOpenIddictRevocationManager>();
        revocationManager.PruneTokensAsync(100, true, Arg.Any<CancellationToken>())
            .Returns(Result.Success(15));

        var services = new ServiceCollection();
        services.AddScoped(_ => revocationManager);
        var serviceProvider = services.BuildServiceProvider();

        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var manager = new TokenCleanupJobManager(scopeFactory);

        var count = await manager.TriggerRunAsync();

        count.Should().Be(15);
        manager.LastPrunedCount.Should().Be(15);
        manager.TotalPrunedCount.Should().Be(15);
    }
}
