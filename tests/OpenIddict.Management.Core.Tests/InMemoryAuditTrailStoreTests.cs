using FluentAssertions;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Models;
using OpenIddict.Management.Services;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class InMemoryAuditTrailStoreTests
{
    [Fact]
    public async Task RecordAsync_And_GetByIdAsync_ReturnsRecordedEntry()
    {
        var store = new InMemoryAuditTrailStore();
        var entry = new ManagementAuditEntry
        {
            Category = "Application",
            Action = "Created",
            EntityId = "app-1",
            EntityName = "Test App",
            Actor = "admin@example.com",
            Details = "Created test app",
            Success = true
        };

        await store.RecordAsync(entry);

        var result = await store.GetByIdAsync(entry.Id);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(entry.Id);
        result.Value.Category.Should().Be("Application");
        result.Value.Action.Should().Be("Created");
        result.Value.Actor.Should().Be("admin@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsFailure()
    {
        var store = new InMemoryAuditTrailStore();
        var result = await store.GetByIdAsync("non-existent");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task QueryAsync_WithFilters_FiltersCorrectly()
    {
        var store = new InMemoryAuditTrailStore();

        await store.RecordAsync(new ManagementAuditEntry
        {
            Category = "Application",
            Action = "Created",
            EntityId = "app-1",
            Actor = "alice",
            Success = true
        });

        await store.RecordAsync(new ManagementAuditEntry
        {
            Category = "Scope",
            Action = "Created",
            EntityId = "scope-1",
            Actor = "bob",
            Success = true
        });

        await store.RecordAsync(new ManagementAuditEntry
        {
            Category = "Application",
            Action = "Deleted",
            EntityId = "app-2",
            Actor = "alice",
            Success = false,
            ErrorMessage = "Not found"
        });

        // Filter by Category
        var catResult = await store.QueryAsync(new AuditFilterRequest { Category = "Application" });
        catResult.IsSuccess.Should().BeTrue();
        catResult.Value!.TotalCount.Should().Be(2);

        // Filter by Actor
        var actorResult = await store.QueryAsync(new AuditFilterRequest { Actor = "alice" });
        actorResult.IsSuccess.Should().BeTrue();
        actorResult.Value!.TotalCount.Should().Be(2);

        // Filter by Success = false
        var failedResult = await store.QueryAsync(new AuditFilterRequest { Success = false });
        failedResult.IsSuccess.Should().BeTrue();
        failedResult.Value!.TotalCount.Should().Be(1);
        failedResult.Value.Items[0].ErrorMessage.Should().Be("Not found");

        // Filter by Action
        var actionResult = await store.QueryAsync(new AuditFilterRequest { Action = "Deleted" });
        actionResult.IsSuccess.Should().BeTrue();
        actionResult.Value!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_Pagination_WorksCorrectly()
    {
        var store = new InMemoryAuditTrailStore();

        for (int i = 0; i < 15; i++)
        {
            await store.RecordAsync(new ManagementAuditEntry
            {
                Category = "Token",
                Action = "Revoked",
                EntityId = $"token-{i}",
                Success = true
            });
        }

        var page1 = await store.QueryAsync(new AuditFilterRequest { PageIndex = 1, PageSize = 10 });
        page1.IsSuccess.Should().BeTrue();
        page1.Value!.Items.Count.Should().Be(10);
        page1.Value.TotalCount.Should().Be(15);
        page1.Value.TotalPages.Should().Be(2);
        page1.Value.HasNextPage.Should().BeTrue();
        page1.Value.HasPreviousPage.Should().BeFalse();

        var page2 = await store.QueryAsync(new AuditFilterRequest { PageIndex = 2, PageSize = 10 });
        page2.IsSuccess.Should().BeTrue();
        page2.Value!.Items.Count.Should().Be(5);
        page2.Value.HasNextPage.Should().BeFalse();
        page2.Value.HasPreviousPage.Should().BeTrue();
    }
}
