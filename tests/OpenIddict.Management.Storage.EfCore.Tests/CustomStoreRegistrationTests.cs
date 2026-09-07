using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Models;
using OpenIddict.Management.Results;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Extensions;
using OpenIddict.Management.Storage.EfCore.Stores;
using Xunit;

namespace OpenIddict.Management.Storage.EfCore.Tests;

public class CustomStoreRegistrationTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public CustomStoreRegistrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task AddApplicationManagementStore_RegistersCustomDerivedStoreInDI()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddDbContext<TestDbContext>(options => options.UseSqlite(_connection));
        services.AddOpenIddictManagementStores<TestDbContext>();
        services.AddApplicationManagementStore<CustomApplicationStore>();

        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        // Act
        using var testScope = provider.CreateScope();
        var appService = testScope.ServiceProvider.GetRequiredService<IApplicationManagementService>();

        // Assert
        appService.Should().BeOfType<CustomApplicationStore>();

        var createResult = await appService.CreateAsync(new ApplicationCreateDto
        {
            ClientId = "custom-override-client",
            DisplayName = "Custom App"
        });

        createResult.IsSuccess.Should().BeTrue();
        createResult.Value.Description.Should().Be("Overridden by CustomApplicationStore");
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private class CustomApplicationStore(TestDbContext dbContext, TimeProvider timeProvider)
        : EfCoreApplicationManagementStore<TestDbContext, Guid>(dbContext, timeProvider)
    {
        public override async Task<Result<ManagedApplication>> CreateAsync(ApplicationCreateDto dto, CancellationToken cancellationToken = default)
        {
            var dtoWithCustomDescription = dto with
            {
                Description = "Overridden by CustomApplicationStore"
            };

            return await base.CreateAsync(dtoWithCustomDescription, cancellationToken);
        }
    }

    private class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<ManagementApplication> Applications => Set<ManagementApplication>();
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.UseOpenIddictManagement();
    }
}
