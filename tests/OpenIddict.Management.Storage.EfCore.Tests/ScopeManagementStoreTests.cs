using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Extensions;
using OpenIddict.Management.Storage.EfCore.Stores;
using Xunit;

namespace OpenIddict.Management.Storage.EfCore.Tests;

public class ScopeManagementStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ScopeTestDbContext> _options;

    public ScopeManagementStoreTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ScopeTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new ScopeTestDbContext(_options);
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task CreateAsync_ValidScope_ReturnsSuccess()
    {
        await using var context = new ScopeTestDbContext(_options);
        var service = new EfCoreScopeManagementStore<ScopeTestDbContext, Guid>(context, TimeProvider.System);

        var result = await service.CreateAsync("api_read", "API Read", "Read access", ["rs_api"]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("api_read");
        result.Value.DisplayName.Should().Be("API Read");
        result.Value.Resources.Should().Contain("rs_api");
    }

    [Fact]
    public async Task CreateAsync_DuplicateScopeName_ReturnsFailure()
    {
        await using var context = new ScopeTestDbContext(_options);
        var service = new EfCoreScopeManagementStore<ScopeTestDbContext, Guid>(context, TimeProvider.System);

        await service.CreateAsync("duplicate_scope", "First Scope", null);
        var result = await service.CreateAsync("duplicate_scope", "Second Scope", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Header.Should().Be("DuplicateEntity");
    }

    [Fact]
    public async Task GetByIdAsync_And_GetByNameAsync_ReturnsScope()
    {
        await using var context = new ScopeTestDbContext(_options);
        var service = new EfCoreScopeManagementStore<ScopeTestDbContext, Guid>(context, TimeProvider.System);

        var created = await service.CreateAsync("identity_profile", "Profile", "User profile info");
        created.IsSuccess.Should().BeTrue();

        var byId = await service.GetByIdAsync(created.Value.Id);
        byId.IsSuccess.Should().BeTrue();
        byId.Value.Name.Should().Be("identity_profile");

        var byName = await service.GetByNameAsync("identity_profile");
        byName.IsSuccess.Should().BeTrue();
        byName.Value.Id.Should().Be(created.Value.Id);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesScope()
    {
        await using var context = new ScopeTestDbContext(_options);
        var service = new EfCoreScopeManagementStore<ScopeTestDbContext, Guid>(context, TimeProvider.System);

        var created = await service.CreateAsync("test_update", "Initial Name", "Initial Description");
        var updateResult = await service.UpdateAsync(created.Value.Id, "Updated Name", "Updated Description", ["res1"]);

        updateResult.IsSuccess.Should().BeTrue();
        updateResult.Value.DisplayName.Should().Be("Updated Name");
        updateResult.Value.Description.Should().Be("Updated Description");
        updateResult.Value.Resources.Should().Contain("res1");
    }

    [Fact]
    public async Task DeleteAsync_RemovesScope()
    {
        await using var context = new ScopeTestDbContext(_options);
        var service = new EfCoreScopeManagementStore<ScopeTestDbContext, Guid>(context, TimeProvider.System);

        var created = await service.CreateAsync("test_delete", "To Delete", null);
        var deleteResult = await service.DeleteAsync(created.Value.Id);

        deleteResult.IsSuccess.Should().BeTrue();

        var fetched = await service.GetByIdAsync(created.Value.Id);
        fetched.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ListAsync_ReturnsPaginatedScopes()
    {
        await using var context = new ScopeTestDbContext(_options);
        var service = new EfCoreScopeManagementStore<ScopeTestDbContext, Guid>(context, TimeProvider.System);

        await service.CreateAsync("scope_a", "Scope A", null);
        await service.CreateAsync("scope_b", "Scope B", null);

        var listResult = await service.ListAsync(new PagedRequest { PageIndex = 1, PageSize = 10, Search = "scope_" });

        listResult.IsSuccess.Should().BeTrue();
        listResult.Value.TotalCount.Should().Be(2);
        listResult.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithDto_ReturnsSuccess()
    {
        await using var context = new ScopeTestDbContext(_options);
        var service = new EfCoreScopeManagementStore<ScopeTestDbContext, Guid>(context, TimeProvider.System);

        var request = new CreateScopeRequest
        {
            Name = "api_dto",
            DisplayName = "API DTO Scope",
            Description = "Created with DTO",
            Resources = ["rs_dto"]
        };

        var result = await service.CreateAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("api_dto");
        result.Value.DisplayName.Should().Be("API DTO Scope");
        result.Value.Resources.Should().Contain("rs_dto");
    }

    [Fact]
    public async Task UpdateAsync_WithDto_ReturnsSuccess()
    {
        await using var context = new ScopeTestDbContext(_options);
        var service = new EfCoreScopeManagementStore<ScopeTestDbContext, Guid>(context, TimeProvider.System);

        var created = await service.CreateAsync(new CreateScopeRequest { Name = "scope_to_update" });
        created.IsSuccess.Should().BeTrue();

        var updateRequest = new UpdateScopeRequest
        {
            DisplayName = "Updated Display Name",
            Description = "Updated Description",
            Resources = ["res1", "res2"]
        };

        var updateResult = await service.UpdateAsync(created.Value.Id, updateRequest);

        updateResult.IsSuccess.Should().BeTrue();
        updateResult.Value.DisplayName.Should().Be("Updated Display Name");
        updateResult.Value.Description.Should().Be("Updated Description");
        updateResult.Value.Resources.Should().Contain("res1");
    }

    [Fact]
    public async Task BulkCreateAsync_And_BulkDeleteAsync_Succeeds()
    {
        await using var context = new ScopeTestDbContext(_options);
        var service = new EfCoreScopeManagementStore<ScopeTestDbContext, Guid>(context, TimeProvider.System);

        var bulkCreate = await service.BulkCreateAsync([
            new CreateScopeRequest { Name = "bulk_1", DisplayName = "Bulk 1" },
            new CreateScopeRequest { Name = "bulk_2", DisplayName = "Bulk 2" }
        ]);

        bulkCreate.IsSuccess.Should().BeTrue();
        bulkCreate.Value.SuccessCount.Should().Be(2);

        var list = await service.ListAsync(new PagedRequest { PageSize = 10 });
        list.Value.TotalCount.Should().Be(2);

        var ids = list.Value.Items.Select(x => x.Id).ToList();
        var bulkDelete = await service.BulkDeleteAsync(ids);

        bulkDelete.IsSuccess.Should().BeTrue();
        bulkDelete.Value.SuccessCount.Should().Be(2);

        var listAfter = await service.ListAsync(new PagedRequest { PageSize = 10 });
        listAfter.Value.TotalCount.Should().Be(0);
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private class ScopeTestDbContext(DbContextOptions<ScopeTestDbContext> options) : DbContext(options)
    {
        public DbSet<ManagementScope> Scopes => Set<ManagementScope>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseOpenIddictManagement();
        }
    }
}
