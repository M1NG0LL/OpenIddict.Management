using FluentAssertions;
using NSubstitute;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Models;
using OpenIddict.Management.Results;
using OpenIddict.Management.Services;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class ResultExtensionsAndFunctionalTests
{
    [Fact]
    public void Result_Map_WhenSuccess_TransformsValue()
    {
        var result = Result<int>.Success(42);

        var mapped = result.Map(x => $"Value: {x}");

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("Value: 42");
    }

    [Fact]
    public void Result_Map_WhenFailure_PreservesError()
    {
        var error = ManagementError.Custom("ERR", "Failed");
        var result = Result<int>.Failure(error);

        var mapped = result.Map(x => $"Value: {x}");

        mapped.IsFailure.Should().BeTrue();
        mapped.Error.Should().Be(error);
    }

    [Fact]
    public async Task Result_MapAsync_WhenSuccess_TransformsValue()
    {
        var result = Result<int>.Success(42);

        var mapped = await result.MapAsync(async x =>
        {
            await Task.Yield();
            return $"Async: {x}";
        });

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("Async: 42");
    }

    [Fact]
    public void Result_Bind_WhenSuccess_ReturnsNewResult()
    {
        var result = Result<int>.Success(10);

        var bound = result.Bind(x => Result<string>.Success($"Bound: {x * 2}"));

        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("Bound: 20");
    }

    [Fact]
    public void Result_Bind_WhenFailure_ReturnsFailure()
    {
        var error = ManagementError.Custom("ERR", "Failed");
        var result = Result<int>.Failure(error);

        var bound = result.Bind(x => Result<string>.Success($"Bound: {x}"));

        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be(error);
    }

    [Fact]
    public async Task Result_BindAsync_WhenSuccess_ReturnsNewResult()
    {
        var result = Result<int>.Success(10);

        var bound = await result.BindAsync(async x =>
        {
            await Task.Yield();
            return Result<string>.Success($"AsyncBound: {x}");
        });

        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("AsyncBound: 10");
    }

    [Fact]
    public void Result_Tap_WhenSuccess_ExecutesAction()
    {
        var result = Result<int>.Success(100);
        var sideEffect = 0;

        result.Tap(v => sideEffect = v);

        sideEffect.Should().Be(100);
    }

    [Fact]
    public void Result_Match_ExecutesCorrectBranch()
    {
        var success = Result<int>.Success(123);
        var failure = Result<int>.Failure(ManagementError.Custom("ERR", "Test"));

        var sResult = success.Match(val => $"OK:{val}", err => "FAIL");
        var fResult = failure.Match(val => "OK", err => $"FAIL:{err.Code}");

        sResult.Should().Be("OK:123");
        fResult.Should().Be("FAIL:ERR");
    }

    [Fact]
    public void TokenListDto_IsExpiredAt_WithTimeProvider_EvaluatesCorrectly()
    {
        var fakeTime = new TestTimeProvider(new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero));

        var validToken = new TokenListDto
        {
            Id = "t1",
            ExpirationDate = new DateTimeOffset(2026, 9, 17, 13, 0, 0, TimeSpan.Zero)
        };

        var expiredToken = new TokenListDto
        {
            Id = "t2",
            ExpirationDate = new DateTimeOffset(2026, 9, 17, 11, 0, 0, TimeSpan.Zero)
        };

        validToken.IsExpiredAt(fakeTime).Should().BeFalse();
        expiredToken.IsExpiredAt(fakeTime).Should().BeTrue();
    }

    [Fact]
    public void TokenListDto_CreatedAt_And_CreationDate_AreHarmonized()
    {
        var now = DateTimeOffset.UtcNow;
        var dto = new TokenListDto
        {
            Id = "t1",
            CreatedAt = now
        };

        dto.CreatedAt.Should().Be(now);
        dto.CreationDate.Should().Be(now);
    }

    [Fact]
    public void ManagedToken_CreatedAt_And_CreationDate_AreHarmonized()
    {
        var now = DateTimeOffset.UtcNow;
        var token = new ManagedToken
        {
            Id = "t1",
            ApplicationId = "app1",
            CreatedAt = now
        };

        token.CreatedAt.Should().Be(now);
        token.CreationDate.Should().Be(now);
    }

    [Fact]
    public async Task OpenIddictTokenService_ResolveDefaultScopesAsync_RethrowsOperationCanceledException()
    {
        var appService = Substitute.For<IApplicationManagementService>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        appService.GetByClientIdAsync("client1", cts.Token)
            .Returns<Task<Result<ManagedApplication>>>(_ => throw new OperationCanceledException(cts.Token));

        var service = new OpenIddictTokenService(appService);

        var act = async () => await service.CreatePrincipalAsync(new TokenCreationParameters
        {
            Subject = "user1",
            ClientId = "client1",
            Scopes = ["openid"]
        }, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task OpenIddictTokenService_ResolveDefaultScopesAsync_SwallowsGeneralExceptionsGracefully()
    {
        var appService = Substitute.For<IApplicationManagementService>();

        appService.GetByClientIdAsync("client1", Arg.Any<CancellationToken>())
            .Returns<Task<Result<ManagedApplication>>>(_ => throw new InvalidOperationException("DB offline"));

        var service = new OpenIddictTokenService(appService);

        // Should not throw, should gracefully fall back to empty default scopes
        var principal = await service.CreatePrincipalAsync(new TokenCreationParameters
        {
            Subject = "user1",
            ClientId = "client1",
            Scopes = ["openid"]
        });

        principal.Should().NotBeNull();
    }

    [Fact]
    public void TokenCleanupJobManager_Dispose_CanBeCalledSafelyMultipleTimes()
    {
        var scopeFactory = Substitute.For<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
        var manager = new TokenCleanupJobManager(scopeFactory);

        manager.Dispose();
        manager.Dispose(); // Should not throw

        // WakeUp after dispose should not throw ObjectDisposedException
        var act = () => manager.WakeUp();
        act.Should().NotThrow();
    }

    private class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
