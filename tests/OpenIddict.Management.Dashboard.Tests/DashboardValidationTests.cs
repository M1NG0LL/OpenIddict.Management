using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dashboard;
using OpenIddict.Management.Dashboard.Pages.Applications;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Extensions;
using Xunit;
using ScopeEditModel = OpenIddict.Management.Dashboard.Pages.Scopes.EditModel;

namespace OpenIddict.Management.Dashboard.Tests;

public class DashboardValidationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;

    public DashboardValidationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(opts => opts.UseSqlite(_connection));
        services.AddOpenIddictManagementStores<TestDbContext>();

        var dashboardOpts = new DashboardOptions();
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(dashboardOpts));

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task CreateApplication_WithRelativeRedirectUri_FailsValidation()
    {
        var appService = _serviceProvider.GetRequiredService<IApplicationManagementService>();
        var scopeService = _serviceProvider.GetRequiredService<IScopeManagementService>();
        var options = _serviceProvider.GetRequiredService<IOptions<DashboardOptions>>();

        var model = new CreateModel(appService, scopeService, options)
        {
            ClientId = "valid-client-id",
            DisplayName = "Valid Display Name",
            RedirectUrisText = "/relative/callback\nhttps://example.com/callback"
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        model.ModelState.IsValid.Should().BeFalse();
        model.ModelState[nameof(model.RedirectUrisText)].Should().NotBeNull();
        model.ModelState[nameof(model.RedirectUrisText)]!.Errors[0].ErrorMessage
            .Should().Contain("not a valid absolute URI");
    }

    [Fact]
    public async Task CreateApplication_WithWhitespaceClientId_FailsValidation()
    {
        var appService = _serviceProvider.GetRequiredService<IApplicationManagementService>();
        var scopeService = _serviceProvider.GetRequiredService<IScopeManagementService>();
        var options = _serviceProvider.GetRequiredService<IOptions<DashboardOptions>>();

        var model = new CreateModel(appService, scopeService, options)
        {
            ClientId = "client with spaces",
            DisplayName = "Valid Display Name"
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        model.ModelState.IsValid.Should().BeFalse();
        model.ModelState[nameof(model.ClientId)].Should().NotBeNull();
        model.ModelState[nameof(model.ClientId)]!.Errors[0].ErrorMessage
            .Should().Contain("cannot contain whitespace");
    }

    [Fact]
    public async Task EditApplication_WithInvalidRedirectUri_FailsValidation()
    {
        var appService = _serviceProvider.GetRequiredService<IApplicationManagementService>();
        var scopeService = _serviceProvider.GetRequiredService<IScopeManagementService>();
        var options = _serviceProvider.GetRequiredService<IOptions<DashboardOptions>>();

        var model = new EditModel(appService, scopeService, options)
        {
            Id = "app-id",
            DisplayName = "Updated Name",
            RedirectUrisText = "not-an-absolute-uri"
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        model.ModelState.IsValid.Should().BeFalse();
        model.ModelState[nameof(model.RedirectUrisText)].Should().NotBeNull();
        model.ModelState[nameof(model.RedirectUrisText)]!.Errors[0].ErrorMessage
            .Should().Contain("not a valid absolute URI");
    }

    [Fact]
    public async Task CreateScope_WithWhitespaceName_FailsValidation()
    {
        var scopeService = _serviceProvider.GetRequiredService<IScopeManagementService>();

        var model = new ScopeEditModel(scopeService)
        {
            Name = "scope with spaces",
            DisplayName = "Scope Display Name"
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        model.ModelState.IsValid.Should().BeFalse();
        model.ModelState[nameof(model.Name)].Should().NotBeNull();
        model.ModelState[nameof(model.Name)]!.Errors[0].ErrorMessage
            .Should().Contain("cannot contain whitespace");
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<ManagementApplication> Applications => Set<ManagementApplication>();
        public DbSet<ManagementScope> Scopes => Set<ManagementScope>();
        public DbSet<ManagementToken> Tokens => Set<ManagementToken>();
        public DbSet<ManagementAuthorization> Authorizations => Set<ManagementAuthorization>();
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.UseOpenIddictManagement();
    }
}
