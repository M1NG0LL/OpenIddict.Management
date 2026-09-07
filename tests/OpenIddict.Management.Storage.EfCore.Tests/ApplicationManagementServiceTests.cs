using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Management.Contracts;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Storage.EfCore.Entities;
using OpenIddict.Management.Storage.EfCore.Extensions;
using OpenIddict.Management.Storage.EfCore.Stores;
using Xunit;

namespace OpenIddict.Management.Storage.EfCore.Tests;

public class ApplicationManagementServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TestDbContext> _options;

    public ApplicationManagementServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new TestDbContext(_options);
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsSuccessResult()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System);

        var dto = new ApplicationCreateDto
        {
            ClientId = "app-client-1",
            DisplayName = "Test Application",
            Environment = ApplicationEnvironment.Staging,
            AllowedRoles = ["Admin", "Manager"]
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var app = result.Value;
        app.ClientId.Should().Be("app-client-1");
        app.DisplayName.Should().Be("Test Application");
        app.Environment.Should().Be(ApplicationEnvironment.Staging);
        app.Status.Should().Be(ApplicationStatus.Active);
        app.AllowedRoles.Should().BeEquivalentTo(["Admin", "Manager"]);
    }

    [Fact]
    public async Task CreateAsync_DuplicateClientId_ReturnsFailureResultWithDuplicateEntityHeader()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System);

        var dto = new ApplicationCreateDto
        {
            ClientId = "duplicate-client",
            DisplayName = "First Client"
        };

        await service.CreateAsync(dto);

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Header.Should().Be("DuplicateEntity");
        result.Error.Description.Should().Contain("duplicate-client");
    }

    [Fact]
    public async Task DeleteAsync_DefaultSoftDelete_SetsStatusToSoftDeleted()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System);

        var createResult = await service.CreateAsync(new ApplicationCreateDto
        {
            ClientId = "soft-delete-app",
            DisplayName = "Soft Delete App"
        });

        var created = createResult.Value;

        // Act: Delete on Active application soft-deletes it to Deleted
        var deleteResult = await service.DeleteAsync(created.Id, hardDelete: false);
        deleteResult.IsSuccess.Should().BeTrue();

        // Assert: Status is Deleted
        var fetchedResult = await service.GetByIdAsync(created.Id);
        fetchedResult.IsSuccess.Should().BeTrue();
        fetchedResult.Value.Status.Should().Be(ApplicationStatus.Deleted);

        // Assert: Default ListAsync does NOT return soft-deleted applications
        var defaultList = await service.ListAsync(new PagedRequest());
        defaultList.IsSuccess.Should().BeTrue();
        defaultList.Value.Items.Should().NotContain(a => a.ClientId == "soft-delete-app");

        // Assert: ListAsync with statusFilter == Deleted DOES return soft-deleted applications
        var deletedList = await service.ListAsync(new PagedRequest(), statusFilter: ApplicationStatus.Deleted);
        deletedList.IsSuccess.Should().BeTrue();
        deletedList.Value.Items.Should().ContainSingle(a => a.ClientId == "soft-delete-app");

        // Act 2: Hard delete permanently removes it
        var deleteHardResult = await service.DeleteAsync(created.Id, hardDelete: true);
        deleteHardResult.IsSuccess.Should().BeTrue();

        var notFoundResult = await service.GetByIdAsync(created.Id);
        notFoundResult.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task SetStatusByEnvironmentAsync_UpdatesMatchingApplications()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System);

        await service.CreateAsync(new ApplicationCreateDto
        {
            ClientId = "staging-app-1",
            DisplayName = "Staging 1",
            Environment = ApplicationEnvironment.Staging
        });
        await service.CreateAsync(new ApplicationCreateDto
        {
            ClientId = "staging-app-2",
            DisplayName = "Staging 2",
            Environment = ApplicationEnvironment.Staging
        });

        // Act
        var result = await service.SetStatusByEnvironmentAsync(ApplicationEnvironment.Staging, ApplicationStatus.Disabled);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);

        var app1 = await service.GetByClientIdAsync("staging-app-1");
        app1.Value.Status.Should().Be(ApplicationStatus.Disabled);
    }

    [Fact]
    public async Task CreateAsync_WithExtraDataAndTags_StoresAndRetrievesSuccessfully()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System);

        var dto = new ApplicationCreateDto
        {
            ClientId = "app-with-extra",
            DisplayName = "Extra Data & Tags App",
            ExtraData = "{\"department\":\"engineering\",\"tier\":\"gold\"}",
            Tags = ["Internal", "Mobile", "FinTech"]
        };

        // Act
        var createResult = await service.CreateAsync(dto);

        // Assert
        createResult.IsSuccess.Should().BeTrue();
        createResult.Value.ExtraData.Should().Be("{\"department\":\"engineering\",\"tier\":\"gold\"}");
        createResult.Value.Tags.Should().BeEquivalentTo(["Internal", "Mobile", "FinTech"]);
        createResult.Value.HasTag("mobile").Should().BeTrue();
        createResult.Value.HasTag("non-existent").Should().BeFalse();

        var getResult = await service.GetByIdAsync(createResult.Value.Id);
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.ExtraData.Should().Be("{\"department\":\"engineering\",\"tier\":\"gold\"}");
        getResult.Value.Tags.Should().BeEquivalentTo(["Internal", "Mobile", "FinTech"]);
        getResult.Value.HasTag("internal").Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExtraDataAndTagsSuccessfully()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System);

        var createResult = await service.CreateAsync(new ApplicationCreateDto
        {
            ClientId = "update-tags-app",
            DisplayName = "Initial Display Name",
            ExtraData = "initial-extra",
            Tags = ["Tag1"]
        });

        var created = createResult.Value;

        // Act
        var updateResult = await service.UpdateAsync(created.Id, new ApplicationUpdateDto
        {
            DisplayName = "Updated Display Name",
            ExtraData = "updated-extra",
            Tags = ["Tag1", "Tag2", "Tag3"]
        });

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        updateResult.Value.ExtraData.Should().Be("updated-extra");
        updateResult.Value.Tags.Should().BeEquivalentTo(["Tag1", "Tag2", "Tag3"]);

        var getResult = await service.GetByIdAsync(created.Id);
        getResult.Value.ExtraData.Should().Be("updated-extra");
        getResult.Value.Tags.Should().BeEquivalentTo(["Tag1", "Tag2", "Tag3"]);
    }

    [Fact]
    public async Task ListAsync_FilterByTags_ReturnsMatchingApplications()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System);

        await service.CreateAsync(new ApplicationCreateDto
        {
            ClientId = "app-tag-a",
            DisplayName = "App A",
            Tags = ["Alpha", "Beta"]
        });
        await service.CreateAsync(new ApplicationCreateDto
        {
            ClientId = "app-tag-b",
            DisplayName = "App B",
            Tags = ["Beta", "Gamma"]
        });
        await service.CreateAsync(new ApplicationCreateDto
        {
            ClientId = "app-tag-c",
            DisplayName = "App C",
            Tags = ["Gamma"]
        });

        // Act & Assert 1: filter by Alpha
        var alphaList = await service.ListAsync(new PagedRequest(), tags: ["Alpha"]);
        alphaList.IsSuccess.Should().BeTrue();
        alphaList.Value.Items.Should().ContainSingle(a => a.ClientId == "app-tag-a");

        // Act & Assert 2: filter by Beta
        var betaList = await service.ListAsync(new PagedRequest(), tags: ["Beta"]);
        betaList.IsSuccess.Should().BeTrue();
        betaList.Value.Items.Should().HaveCount(2);
        betaList.Value.Items.Select(a => a.ClientId).Should().BeEquivalentTo(["app-tag-a", "app-tag-b"]);
    }

    [Fact]
    public async Task CreateAsync_WithClientSecret_WhenApplicationManagerIsNull_ThrowsInvalidOperationException()
    {
        // Arrange: instantiate without applicationManager
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System, applicationManager: null);

        var dto = new ApplicationCreateDto
        {
            ClientId = "confidential-client-no-mgr",
            DisplayName = "Confidential Client Without Manager",
            ClientSecret = "plain-secret-value"
        };

        // Act
        var act = async () => await service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*IOpenIddictApplicationManager is required*");
    }

    [Fact]
    public async Task CreateAsync_WithClientSecret_WhenApplicationManagerProvided_HashesSecretAndSetsConfidential()
    {
        // Arrange
        var (service, appManager, sp) = CreateServiceWithDi();
        using (sp)
        {
            var dto = new ApplicationCreateDto
            {
                ClientId = "confidential-client-with-mgr",
                DisplayName = "Confidential Client With Manager",
                ClientSecret = "super-secret-password-123",
                RedirectUris = ["https://localhost/callback"],
                Permissions = [OpenIddict.Abstractions.OpenIddictConstants.Permissions.Endpoints.Token]
            };

            // Act
            var createResult = await service.CreateAsync(dto);

            // Assert
            createResult.IsSuccess.Should().BeTrue();
            var created = createResult.Value;

            // Verify via OpenIddict ApplicationManager that the secret was hashed and validates correctly
            var appEntity = await appManager.FindByIdAsync(created.Id);
            appEntity.Should().NotBeNull();
            (await appManager.GetClientTypeAsync(appEntity!)).Should().Be(OpenIddict.Abstractions.OpenIddictConstants.ClientTypes.Confidential);

            var isValidSecret = await appManager.ValidateClientSecretAsync(appEntity!, "super-secret-password-123");
            isValidSecret.Should().BeTrue();

            var isWrongSecretValid = await appManager.ValidateClientSecretAsync(appEntity!, "wrong-password");
            isWrongSecretValid.Should().BeFalse();
        }
    }

    [Fact]
    public async Task CreateAsync_ValidationFailures_ReturnsValidationError()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System);

        // Act 1: null DTO
        var nullResult = await service.CreateAsync(null!);
        nullResult.IsFailure.Should().BeTrue();
        nullResult.Error!.Header.Should().Be("ValidationError");

        // Act 2: empty ClientId
        var emptyClientIdResult = await service.CreateAsync(new ApplicationCreateDto { ClientId = "   ", DisplayName = "Invalid Client" });
        emptyClientIdResult.IsFailure.Should().BeTrue();
        emptyClientIdResult.Error!.Header.Should().Be("ValidationError");
    }

    [Fact]
    public async Task GetByIdAsync_And_GetByClientIdAsync_ValidationAndNotFound()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System);

        // Act & Assert GetByIdAsync with empty ID
        var emptyIdResult = await service.GetByIdAsync("   ");
        emptyIdResult.IsFailure.Should().BeTrue();
        emptyIdResult.Error!.Header.Should().Be("ValidationError");

        // Act & Assert GetByIdAsync with non-existent ID
        var notFoundIdResult = await service.GetByIdAsync(Guid.NewGuid().ToString());
        notFoundIdResult.IsFailure.Should().BeTrue();
        notFoundIdResult.Error!.Header.Should().Be("EntityNotFound");

        // Act & Assert GetByClientIdAsync with empty ClientId
        var emptyClientResult = await service.GetByClientIdAsync("   ");
        emptyClientResult.IsFailure.Should().BeTrue();
        emptyClientResult.Error!.Header.Should().Be("ValidationError");

        // Act & Assert GetByClientIdAsync with non-existent ClientId
        var notFoundClientResult = await service.GetByClientIdAsync("non-existent-client-id");
        notFoundClientResult.IsFailure.Should().BeTrue();
        notFoundClientResult.Error!.Header.Should().Be("EntityNotFound");
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_UpdatesAllFieldsCorrectly()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System);

        var created = (await service.CreateAsync(new ApplicationCreateDto
        {
            ClientId = "update-target-client",
            DisplayName = "Initial Name",
            Environment = ApplicationEnvironment.Development,
            Description = "Initial Description",
            LogoUri = "https://example.com/initial.png",
            OwnerUserId = "user-1",
            AllowedRoles = ["Reader"]
        })).Value;

        var updateDto = new ApplicationUpdateDto
        {
            DisplayName = "Updated Name",
            Environment = ApplicationEnvironment.Production,
            Status = ApplicationStatus.Active,
            Description = "Updated Description",
            LogoUri = "https://example.com/updated.png",
            OwnerUserId = "user-2",
            ExtraData = "{\"version\":2}",
            Tags = ["Production", "Core"],
            AllowedRoles = ["Admin", "SuperUser"],
            RedirectUris = ["https://prod.example.com/callback"],
            PostLogoutRedirectUris = ["https://prod.example.com/signout"],
            Permissions = ["ept:token", "gt:authorization_code"],
            Requirements = ["features:pkce"]
        };

        // Act
        var updateResult = await service.UpdateAsync(created.Id, updateDto);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        var updated = updateResult.Value;
        updated.DisplayName.Should().Be("Updated Name");
        updated.Environment.Should().Be(ApplicationEnvironment.Production);
        updated.Description.Should().Be("Updated Description");
        updated.LogoUri.Should().Be("https://example.com/updated.png");
        updated.OwnerUserId.Should().Be("user-2");
        updated.ExtraData.Should().Be("{\"version\":2}");
        updated.Tags.Should().BeEquivalentTo(["Production", "Core"]);
        updated.AllowedRoles.Should().BeEquivalentTo(["Admin", "SuperUser"]);
        updated.RedirectUris.Should().BeEquivalentTo(["https://prod.example.com/callback"]);
        updated.PostLogoutRedirectUris.Should().BeEquivalentTo(["https://prod.example.com/signout"]);
        updated.Permissions.Should().BeEquivalentTo(["ept:token", "gt:authorization_code"]);
        updated.Requirements.Should().BeEquivalentTo(["features:pkce"]);
        updated.LastModifiedAt.Should().NotBeNull();

        // Verify fresh retrieval from DB
        var fetched = (await service.GetByIdAsync(created.Id)).Value;
        fetched.DisplayName.Should().Be("Updated Name");
        fetched.Environment.Should().Be(ApplicationEnvironment.Production);
    }

    [Fact]
    public async Task UpdateAsync_ValidationFailures_ReturnsExpectedError()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System);

        // Empty ID
        var emptyId = await service.UpdateAsync("  ", new ApplicationUpdateDto { DisplayName = "Test" });
        emptyId.IsFailure.Should().BeTrue();
        emptyId.Error!.Header.Should().Be("ValidationError");

        // Null DTO
        var nullDto = await service.UpdateAsync(Guid.NewGuid().ToString(), null!);
        nullDto.IsFailure.Should().BeTrue();
        nullDto.Error!.Header.Should().Be("ValidationError");

        // Non-existent ID
        var notFound = await service.UpdateAsync(Guid.NewGuid().ToString(), new ApplicationUpdateDto { DisplayName = "Test" });
        notFound.IsFailure.Should().BeTrue();
        notFound.Error!.Header.Should().Be("EntityNotFound");
    }

    [Fact]
    public async Task UpdateClientSecretAsync_WhenApplicationManagerIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System, applicationManager: null);

        var created = (await service.CreateAsync(new ApplicationCreateDto
        {
            ClientId = "secret-test-no-mgr",
            DisplayName = "Secret Test"
        })).Value;

        // Act
        var act = async () => await service.UpdateClientSecretAsync(created.Id, "new-secret-value");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*IOpenIddictApplicationManager is required*");
    }

    [Fact]
    public async Task UpdateClientSecretAsync_WithNewSecret_HashesAndValidatesCorrectly()
    {
        // Arrange
        var (service, appManager, sp) = CreateServiceWithDi();
        using (sp)
        {
            var created = (await service.CreateAsync(new ApplicationCreateDto
            {
                ClientId = "secret-update-client",
                DisplayName = "Secret Update Client",
                ClientSecret = "initial-secret-123"
            })).Value;

            // Act 1: Update to a new secret
            var updateSecretResult = await service.UpdateClientSecretAsync(created.Id, "updated-secret-456");
            updateSecretResult.IsSuccess.Should().BeTrue();

            // Assert: OpenIddict validates new secret and rejects old secret
            var appEntity = await appManager.FindByIdAsync(created.Id);
            (await appManager.ValidateClientSecretAsync(appEntity!, "updated-secret-456")).Should().BeTrue();
            (await appManager.ValidateClientSecretAsync(appEntity!, "initial-secret-123")).Should().BeFalse();

            // Act 2: Clear secret (null) to make it a public client
            var clearSecretResult = await service.UpdateClientSecretAsync(created.Id, null);
            clearSecretResult.IsSuccess.Should().BeTrue();

            var appEntityAfterClear = await appManager.FindByIdAsync(created.Id);
            (await appManager.GetClientTypeAsync(appEntityAfterClear!)).Should().Be(OpenIddict.Abstractions.OpenIddictConstants.ClientTypes.Public);
            var entity = (ManagementApplication<Guid>)appEntityAfterClear!;
            entity.ClientSecret.Should().BeNull();
        }
    }

    [Fact]
    public async Task UpdateClientSecretAsync_ValidationFailures_ReturnsExpectedError()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System);

        // Empty ID
        var emptyId = await service.UpdateClientSecretAsync("  ", "any-secret");
        emptyId.IsFailure.Should().BeTrue();
        emptyId.Error!.Header.Should().Be("ValidationError");

        // Non-existent ID
        var notFound = await service.UpdateClientSecretAsync(Guid.NewGuid().ToString(), "any-secret");
        notFound.IsFailure.Should().BeTrue();
        notFound.Error!.Header.Should().Be("EntityNotFound");
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidAndInvalid_UpdatesStatusCorrectly()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System);

        var created = (await service.CreateAsync(new ApplicationCreateDto
        {
            ClientId = "status-test-client",
            DisplayName = "Status Test"
        })).Value;

        // Act: Update to Disabled
        var updateResult = await service.UpdateStatusAsync(created.Id, ApplicationStatus.Disabled);
        updateResult.IsSuccess.Should().BeTrue();

        var fetched = (await service.GetByIdAsync(created.Id)).Value;
        fetched.Status.Should().Be(ApplicationStatus.Disabled);

        // Empty ID
        var emptyId = await service.UpdateStatusAsync("  ", ApplicationStatus.Active);
        emptyId.IsFailure.Should().BeTrue();
        emptyId.Error!.Header.Should().Be("ValidationError");

        // Non-existent ID
        var notFound = await service.UpdateStatusAsync(Guid.NewGuid().ToString(), ApplicationStatus.Active);
        notFound.IsFailure.Should().BeTrue();
        notFound.Error!.Header.Should().Be("EntityNotFound");
    }

    [Fact]
    public async Task SetStatusByEnvironmentAsync_NullEnvironment_UpdatesAllEnvironments()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System);

        await service.CreateAsync(new ApplicationCreateDto { ClientId = "bulk-env-dev", DisplayName = "Dev", Environment = ApplicationEnvironment.Development });
        await service.CreateAsync(new ApplicationCreateDto { ClientId = "bulk-env-staging", DisplayName = "Staging", Environment = ApplicationEnvironment.Staging });
        await service.CreateAsync(new ApplicationCreateDto { ClientId = "bulk-env-prod", DisplayName = "Prod", Environment = ApplicationEnvironment.Production });

        // Act: Pass null for environment to target all applications
        var result = await service.SetStatusByEnvironmentAsync(null, ApplicationStatus.Disabled);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);

        (await service.GetByClientIdAsync("bulk-env-dev")).Value.Status.Should().Be(ApplicationStatus.Disabled);
        (await service.GetByClientIdAsync("bulk-env-staging")).Value.Status.Should().Be(ApplicationStatus.Disabled);
        (await service.GetByClientIdAsync("bulk-env-prod")).Value.Status.Should().Be(ApplicationStatus.Disabled);
    }

    [Fact]
    public async Task ListAsync_SearchAndPaginationAndSorting_FunctionsCorrectly()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System);

        for (var i = 1; i <= 5; i++)
        {
            await service.CreateAsync(new ApplicationCreateDto
            {
                ClientId = $"client-sort-{i:D2}",
                DisplayName = $"App {i:D2}",
                OwnerUserId = $"owner-{i}",
                Environment = i % 2 == 0 ? ApplicationEnvironment.Production : ApplicationEnvironment.Development
            });
        }

        // Search by ClientId
        var searchResult = await service.ListAsync(new PagedRequest { Search = "sort-03" });
        searchResult.IsSuccess.Should().BeTrue();
        searchResult.Value.Items.Should().ContainSingle(a => a.ClientId == "client-sort-03");

        // Pagination
        var pageResult = await service.ListAsync(new PagedRequest { PageIndex = 2, PageSize = 2 });
        pageResult.IsSuccess.Should().BeTrue();
        pageResult.Value.Items.Should().HaveCount(2);
        pageResult.Value.TotalCount.Should().Be(5);
        pageResult.Value.TotalPages.Should().Be(3);

        // Sorting descending by DisplayName
        var sortResult = await service.ListAsync(new PagedRequest { SortBy = "DisplayName", SortDescending = true });
        sortResult.IsSuccess.Should().BeTrue();
        sortResult.Value.Items[0].DisplayName.Should().Be("App 05");

        // Null request validation
        var nullRequestResult = await service.ListAsync(null!);
        nullRequestResult.IsFailure.Should().BeTrue();
        nullRequestResult.Error!.Header.Should().Be("ValidationError");
    }

    [Fact]
    public async Task CompleteApplicationCrudLifecycle_EndToEnd_Succeeds()
    {
        // Arrange
        var (service, appManager, sp) = CreateServiceWithDi();
        using (sp)
        {
            // 1. CREATE
            var createDto = new ApplicationCreateDto
            {
                ClientId = "lifecycle-app-client",
                DisplayName = "Lifecycle Application",
                ClientSecret = "lifecycle-secret-123",
                Environment = ApplicationEnvironment.Development,
                Description = "A test application going through the full lifecycle",
                Tags = ["Lifecycle", "IntegrationTest"],
                AllowedRoles = ["Admin", "Auditor"]
            };

            var createResult = await service.CreateAsync(createDto);
            createResult.IsSuccess.Should().BeTrue();
            var appId = createResult.Value.Id;

            // 2. READ (GetById and GetByClientId)
            var getById = await service.GetByIdAsync(appId);
            getById.IsSuccess.Should().BeTrue();
            getById.Value.ClientId.Should().Be("lifecycle-app-client");
            var entityBeforeUpdate = await appManager.FindByIdAsync(appId);
            (await appManager.GetClientTypeAsync(entityBeforeUpdate!)).Should().Be(OpenIddict.Abstractions.OpenIddictConstants.ClientTypes.Confidential);

            var getByClientId = await service.GetByClientIdAsync("lifecycle-app-client");
            getByClientId.IsSuccess.Should().BeTrue();
            getByClientId.Value.Id.Should().Be(appId);

            // 3. UPDATE
            var updateResult = await service.UpdateAsync(appId, new ApplicationUpdateDto
            {
                DisplayName = "Lifecycle Application (Updated)",
                Environment = ApplicationEnvironment.Staging,
                Tags = ["Lifecycle", "Updated"]
            });
            updateResult.IsSuccess.Should().BeTrue();
            updateResult.Value.DisplayName.Should().Be("Lifecycle Application (Updated)");
            updateResult.Value.Environment.Should().Be(ApplicationEnvironment.Staging);

            // 4. UPDATE CLIENT SECRET
            var secretResult = await service.UpdateClientSecretAsync(appId, "new-lifecycle-secret-789");
            secretResult.IsSuccess.Should().BeTrue();

            var entity = await appManager.FindByIdAsync(appId);
            (await appManager.ValidateClientSecretAsync(entity!, "new-lifecycle-secret-789")).Should().BeTrue();

            // 5. UPDATE STATUS
            var statusResult = await service.UpdateStatusAsync(appId, ApplicationStatus.Disabled);
            statusResult.IsSuccess.Should().BeTrue();

            var disabledApp = (await service.GetByIdAsync(appId)).Value;
            disabledApp.Status.Should().Be(ApplicationStatus.Disabled);

            // 6. SOFT DELETE
            var softDeleteResult = await service.DeleteAsync(appId, hardDelete: false);
            softDeleteResult.IsSuccess.Should().BeTrue();

            var softDeletedApp = (await service.GetByIdAsync(appId)).Value;
            softDeletedApp.Status.Should().Be(ApplicationStatus.Deleted);

            // Excluded from default list
            var listAfterSoftDelete = await service.ListAsync(new PagedRequest());
            listAfterSoftDelete.Value.Items.Should().NotContain(a => a.Id == appId);

            // 7. HARD DELETE
            var hardDeleteResult = await service.DeleteAsync(appId, hardDelete: true);
            hardDeleteResult.IsSuccess.Should().BeTrue();

            var notFoundAfterHardDelete = await service.GetByIdAsync(appId);
            notFoundAfterHardDelete.IsFailure.Should().BeTrue();
            notFoundAfterHardDelete.Error!.Header.Should().Be("EntityNotFound");
        }
    }

    [Fact]
    public async Task DeleteAsync_DeletesAssociatedTokensAndAuthorizations_OnSoftAndHardDelete()
    {
        // Arrange
        await using var context = new TestDbContext(_options);
        var service = new EfCoreApplicationManagementStore<TestDbContext, Guid>(context, TimeProvider.System);

        // 1. Create Application
        var app = (await service.CreateAsync(new ApplicationCreateDto
        {
            ClientId = "cascade-delete-client",
            DisplayName = "Cascade Delete Client"
        })).Value;

        var appEntity = await context.Applications.FirstAsync(a => a.ClientId == "cascade-delete-client");

        // 2. Attach an authorization to this application
        var auth = new ManagementAuthorization<Guid>
        {
            Id = Guid.NewGuid(),
            Application = appEntity,
            Subject = "user-1",
            Status = "valid",
            Type = "permanent",
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Authorizations.Add(auth);

        // 3. Attach tokens (one directly to app, one via authorization)
        var token1 = new ManagementToken<Guid>
        {
            Id = Guid.NewGuid(),
            Application = appEntity,
            Subject = "user-1",
            Type = "access_token",
            Status = "valid",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var token2 = new ManagementToken<Guid>
        {
            Id = Guid.NewGuid(),
            Application = appEntity,
            Authorization = auth,
            Subject = "user-1",
            Type = "refresh_token",
            Status = "valid",
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Tokens.AddRange(token1, token2);
        await context.SaveChangesAsync();

        // Verify tokens & auths exist
        var initialTokens = await context.Tokens.Where(t => t.Application != null && t.Application.ClientId == "cascade-delete-client").CountAsync();
        initialTokens.Should().Be(2);
        var initialAuths = await context.Authorizations.Where(a => a.Application != null && a.Application.ClientId == "cascade-delete-client").CountAsync();
        initialAuths.Should().Be(1);

        // Act: Soft Delete
        var softDeleteResult = await service.DeleteAsync(app.Id, hardDelete: false);
        softDeleteResult.IsSuccess.Should().BeTrue();

        // Assert: Tokens and authorizations are DELETED from database
        var remainingTokens = await context.Tokens.Where(t => t.Application != null && t.Application.ClientId == "cascade-delete-client").CountAsync();
        remainingTokens.Should().Be(0);

        var remainingAuths = await context.Authorizations.Where(a => a.Application != null && a.Application.ClientId == "cascade-delete-client").CountAsync();
        remainingAuths.Should().Be(0);

        // Assert: Application still exists with Status = Deleted
        var softDeletedApp = await context.Applications.FirstAsync(a => a.ClientId == "cascade-delete-client");
        softDeletedApp.Status.Should().Be(ApplicationStatus.Deleted);

        // Act 2: Hard Delete
        var hardDeleteResult = await service.DeleteAsync(app.Id, hardDelete: true);
        hardDeleteResult.IsSuccess.Should().BeTrue();

        // Assert: Application is completely removed
        var appCount = await context.Applications.Where(a => a.ClientId == "cascade-delete-client").CountAsync();
        appCount.Should().Be(0);
    }

    private (IApplicationManagementService Service, OpenIddict.Abstractions.IOpenIddictApplicationManager AppManager, ServiceProvider Provider) CreateServiceWithDi()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddDbContext<TestDbContext>(opts => opts.UseSqlite(_connection));
        services.AddOpenIddictManagementStores<TestDbContext>();

        var sp = services.BuildServiceProvider();
        var service = sp.GetRequiredService<IApplicationManagementService>();
        var appManager = sp.GetRequiredService<OpenIddict.Abstractions.IOpenIddictApplicationManager>();
        return (service, appManager, sp);
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<ManagementApplication<Guid>> Applications => Set<ManagementApplication<Guid>>();
        public DbSet<ManagementAuthorization<Guid>> Authorizations => Set<ManagementAuthorization<Guid>>();
        public DbSet<ManagementToken<Guid>> Tokens => Set<ManagementToken<Guid>>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseOpenIddictManagement();
        }
    }
}
