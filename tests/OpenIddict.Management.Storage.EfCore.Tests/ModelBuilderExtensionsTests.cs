using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Extensions;
using Xunit;

namespace OpenIddict.Management.Storage.EfCore.Tests;

public class ModelBuilderExtensionsTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public ModelBuilderExtensionsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    private class DummyDbContext(DbContextOptions<DummyDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseOpenIddictManagement();
        }
    }

    [Fact]
    public void UseOpenIddictManagement_RegistersExtendedEntitiesInModel()
    {
        var options = new DbContextOptionsBuilder<DummyDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new DummyDbContext(options);
        var model = context.Model;

        model.FindEntityType(typeof(ManagementApplication<Guid>)).Should().NotBeNull();
        model.FindEntityType(typeof(ManagementAuthorization<Guid>)).Should().NotBeNull();
        model.FindEntityType(typeof(ManagementToken<Guid>)).Should().NotBeNull();
        model.FindEntityType(typeof(ManagementScope<Guid>)).Should().NotBeNull();
    }

    [Fact]
    public void OpenIddictIntegration_DatabaseEnsureCreated_Succeeds()
    {
        var services = new ServiceCollection();
        services.AddDbContext<DummyDbContext>(options =>
        {
            options.UseSqlite(_connection);
        });
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<DummyDbContext>();
                options.SetDefaultApplicationEntity(typeof(ManagementApplication<Guid>))
                       .SetDefaultAuthorizationEntity(typeof(ManagementAuthorization<Guid>))
                       .SetDefaultScopeEntity(typeof(ManagementScope<Guid>))
                       .SetDefaultTokenEntity(typeof(ManagementToken<Guid>));
            });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DummyDbContext>();

        var action = () => context.Database.EnsureCreated();
        action.Should().NotThrow();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
