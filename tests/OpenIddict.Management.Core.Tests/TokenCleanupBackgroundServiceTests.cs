using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Services;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class TokenCleanupBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenEnabled_ExecutesJobCycle()
    {
        var jobManager = Substitute.For<ITokenCleanupJobManager>();
        jobManager.IsEnabled.Returns(true);
        jobManager.BatchSize.Returns(100);

        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        using var cts = new CancellationTokenSource();

        jobManager.WaitForNextExecutionAsync(Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await Task.Yield();
                cts.Cancel();
            });

        var backgroundService = new TokenCleanupBackgroundService(jobManager, serviceProvider);

        await backgroundService.StartAsync(cts.Token);
        await Task.Delay(50);
        await backgroundService.StopAsync(CancellationToken.None);

        await jobManager.Received().RunJobCycleAsync(serviceProvider, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_SkipsJobCycle()
    {
        var jobManager = Substitute.For<ITokenCleanupJobManager>();
        jobManager.IsEnabled.Returns(false);

        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        using var cts = new CancellationTokenSource();

        jobManager.WaitForNextExecutionAsync(Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await Task.Yield();
                cts.Cancel();
            });

        var backgroundService = new TokenCleanupBackgroundService(jobManager, serviceProvider);

        await backgroundService.StartAsync(cts.Token);
        await Task.Delay(50);
        await backgroundService.StopAsync(CancellationToken.None);

        await jobManager.DidNotReceive().RunJobCycleAsync(Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>());
    }
}
