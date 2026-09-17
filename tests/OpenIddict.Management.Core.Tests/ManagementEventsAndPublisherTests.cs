using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Events;
using OpenIddict.Management.Models;
using OpenIddict.Management.Options;
using OpenIddict.Management.Services;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class ManagementEventsAndPublisherTests
{
    private sealed class TrackingHandler<T> : IManagementEventHandler<T> where T : IManagementEvent
    {
        public List<T> HandledEvents { get; } = [];

        public Task HandleAsync(T @event, CancellationToken cancellationToken = default)
        {
            HandledEvents.Add(@event);
            return Task.CompletedTask;
        }
    }

    private sealed class FaultyHandler<T> : IManagementEventHandler<T> where T : IManagementEvent
    {
        public Task HandleAsync(T @event, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Handler blew up!");
        }
    }

    [Fact]
    public async Task Publisher_DispatchesEvent_ToRegisteredHandlers()
    {
        var services = new ServiceCollection();
        var handler = new TrackingHandler<ApplicationCreatedEvent>();
        services.AddSingleton<IManagementEventHandler<ApplicationCreatedEvent>>(handler);

        var serviceProvider = services.BuildServiceProvider();
        var options = Microsoft.Extensions.Options.Options.Create(new OpenIddictManagementOptions { EnableAuditLogging = true });
        var publisher = new ManagementEventPublisher(serviceProvider, options, NullLogger<ManagementEventPublisher>.Instance);

        var app = new ManagedApplication { Id = "app-123", ClientId = "client-abc", DisplayName = "My App", CreatedAt = DateTimeOffset.UtcNow };
        var @event = new ApplicationCreatedEvent(app, "admin");
        await publisher.PublishAsync(@event);

        handler.HandledEvents.Should().ContainSingle();
        handler.HandledEvents[0].Application.Id.Should().Be("app-123");
        handler.HandledEvents[0].Application.ClientId.Should().Be("client-abc");
    }

    [Fact]
    public async Task Publisher_WhenHandlerThrows_IsolatesErrorAndContinues()
    {
        var services = new ServiceCollection();
        var goodHandler = new TrackingHandler<ScopeDeletedEvent>();
        services.AddSingleton<IManagementEventHandler<ScopeDeletedEvent>>(new FaultyHandler<ScopeDeletedEvent>());
        services.AddSingleton<IManagementEventHandler<ScopeDeletedEvent>>(goodHandler);

        var serviceProvider = services.BuildServiceProvider();
        var options = Microsoft.Extensions.Options.Options.Create(new OpenIddictManagementOptions { EnableAuditLogging = true });
        var publisher = new ManagementEventPublisher(serviceProvider, options, NullLogger<ManagementEventPublisher>.Instance);

        var @event = new ScopeDeletedEvent("scope-1", "api_read", "admin");
        var act = async () => await publisher.PublishAsync(@event);

        await act.Should().NotThrowAsync();
        goodHandler.HandledEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task AuditTrailEventHandler_RecordsEntriesInStore()
    {
        var store = new InMemoryAuditTrailStore();
        var handler = new AuditTrailEventHandler(store);

        var app = new ManagedApplication { Id = "app-1", ClientId = "client-1", DisplayName = "App 1", CreatedAt = DateTimeOffset.UtcNow };
        var appCreated = new ApplicationCreatedEvent(app, "superadmin");
        await handler.HandleAsync(appCreated);

        var tokenRevoked = new TokenRevokedEvent("tok-999", Actor: "admin");
        await handler.HandleAsync(tokenRevoked);

        var query = await store.QueryAsync(new Dto.AuditFilterRequest());
        query.IsSuccess.Should().BeTrue();
        query.Value!.TotalCount.Should().Be(2);

        var appEntry = query.Value.Items.First(x => x.Category == "Application");
        appEntry.Action.Should().Be("Created");
        appEntry.EntityId.Should().Be("app-1");
        appEntry.Actor.Should().Be("superadmin");

        var tokenEntry = query.Value.Items.First(x => x.Category == "Token");
        tokenEntry.Action.Should().Be("Revoked");
        tokenEntry.EntityId.Should().Be("tok-999");
    }
}
