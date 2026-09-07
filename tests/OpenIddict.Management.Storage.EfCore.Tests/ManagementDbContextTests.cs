using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Extensions;
using Xunit;

namespace OpenIddict.Management.Storage.EfCore.Tests;

public class ManagementDbContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<CustomHostDbContext> _options;

    public ManagementDbContextTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<CustomHostDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    [Fact]
    public async Task CustomHostDbContext_CanCreateDatabaseAndAddManagementApplication()
    {
        // Arrange
        await using (var context = new CustomHostDbContext(_options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var appId = Guid.NewGuid();

        // Act
        await using (var context = new CustomHostDbContext(_options))
        {
            var app = new ManagementApplication
            {
                Id = appId,
                ClientId = "test-client",
                DisplayName = "Test Client",
                Status = ApplicationStatus.Active,
                Environment = ApplicationEnvironment.Development,
                OwnerUserId = "user-123",
                CreatedAt = DateTimeOffset.UtcNow
            };

            app.SetAllowedRoles(["Admin", "User"]);

            context.Applications.Add(app);
            await context.SaveChangesAsync();
        }

        // Assert
        await using (var context = new CustomHostDbContext(_options))
        {
            var app = await context.Applications.FirstOrDefaultAsync(a => a.Id == appId);
            app.Should().NotBeNull();
            app!.ClientId.Should().Be("test-client");
            app.Status.Should().Be(ApplicationStatus.Active);
            app.Environment.Should().Be(ApplicationEnvironment.Development);
            app.OwnerUserId.Should().Be("user-123");

            var roles = app.GetAllowedRoles();
            roles.Should().BeEquivalentTo(["Admin", "User"]);
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private class CustomHostDbContext(DbContextOptions<CustomHostDbContext> options) : DbContext(options)
    {
        public DbSet<ManagementApplication> Applications => Set<ManagementApplication>();
        public DbSet<ManagementAuthorization> Authorizations => Set<ManagementAuthorization>();
        public DbSet<ManagementToken> Tokens => Set<ManagementToken>();
        public DbSet<ManagementScope> Scopes => Set<ManagementScope>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseOpenIddictManagement();
        }
    }
}
