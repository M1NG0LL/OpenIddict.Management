using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Management.Builder;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Extensions;
using OpenIddict.Management.Options;
using OpenIddict.Management.Validation;
using OpenIddict.Server;
using OpenIddict.Validation;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class ApplicationValidationRegistrationTests
{
    [Fact]
    public void OpenIddictManagementBuilder_AddApplicationValidation_RegistersServicesAndHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new OpenIddictManagementBuilder(services);

        // Act
        builder.AddApplicationValidation(options =>
        {
            options.RequireEnvironment(ApplicationEnvironment.Production);
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IOpenIddictApplicationValidator>();
        validator.Should().NotBeNull();
        validator.Should().BeOfType<OpenIddictApplicationValidator>();

        var handler = provider.GetService<OpenIddictApplicationValidationHandler>();
        handler.Should().NotBeNull();

        var options = provider.GetRequiredService<IOptions<OpenIddictApplicationValidationOptions>>().Value;
        options.ValidateEnvironment.Should().BeTrue();
        options.ExpectedEnvironment.Should().Be(ApplicationEnvironment.Production);

        var serverOptions = provider.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
        serverOptions.Handlers.Should().Contain(h =>
            h.ServiceDescriptor != null &&
            (h.ServiceDescriptor.ServiceType == typeof(OpenIddictApplicationValidationHandler) ||
             h.ServiceDescriptor.ImplementationType == typeof(OpenIddictApplicationValidationHandler)));

        var validationOptions = provider.GetRequiredService<IOptions<OpenIddictValidationOptions>>().Value;
        validationOptions.Handlers.Should().Contain(h =>
            h.ServiceDescriptor != null &&
            (h.ServiceDescriptor.ServiceType == typeof(OpenIddictApplicationValidationHandler) ||
             h.ServiceDescriptor.ImplementationType == typeof(OpenIddictApplicationValidationHandler)));
    }

    [Fact]
    public void OpenIddictServerBuilder_AddApplicationValidation_RegistersServerHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var serverBuilder = new OpenIddictServerBuilder(services);

        // Act
        serverBuilder.AddApplicationValidation(options =>
        {
            options.RequireStatus(ApplicationStatus.Active);
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IOpenIddictApplicationValidator>();
        validator.Should().NotBeNull();

        var handler = provider.GetService<OpenIddictApplicationValidationHandler>();
        handler.Should().NotBeNull();

        var serverOptions = provider.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
        serverOptions.Handlers.Should().Contain(h =>
            h.ServiceDescriptor != null &&
            (h.ServiceDescriptor.ServiceType == typeof(OpenIddictApplicationValidationHandler) ||
             h.ServiceDescriptor.ImplementationType == typeof(OpenIddictApplicationValidationHandler)));
    }

    [Fact]
    public void OpenIddictValidationBuilder_AddApplicationValidation_RegistersValidationHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var validationBuilder = new OpenIddictValidationBuilder(services);

        // Act
        validationBuilder.AddApplicationValidation(options =>
        {
            options.RequireEnvironment(ApplicationEnvironment.Staging);
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IOpenIddictApplicationValidator>();
        validator.Should().NotBeNull();

        var handler = provider.GetService<OpenIddictApplicationValidationHandler>();
        handler.Should().NotBeNull();

        var validationOptions = provider.GetRequiredService<IOptions<OpenIddictValidationOptions>>().Value;
        validationOptions.Handlers.Should().Contain(h =>
            h.ServiceDescriptor != null &&
            (h.ServiceDescriptor.ServiceType == typeof(OpenIddictApplicationValidationHandler) ||
             h.ServiceDescriptor.ImplementationType == typeof(OpenIddictApplicationValidationHandler)));
    }

    [Fact]
    public void ServiceCollection_AddOpenIddictApplicationValidation_RegistersAll()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOpenIddictApplicationValidation(options =>
        {
            options.ValidateStatus = true;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IOpenIddictApplicationValidator>().Should().NotBeNull();
        provider.GetService<OpenIddictApplicationValidationHandler>().Should().NotBeNull();

        var serverOptions = provider.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
        serverOptions.Handlers.Should().Contain(h =>
            h.ServiceDescriptor != null &&
            (h.ServiceDescriptor.ServiceType == typeof(OpenIddictApplicationValidationHandler) ||
             h.ServiceDescriptor.ImplementationType == typeof(OpenIddictApplicationValidationHandler)));

        var validationOptions = provider.GetRequiredService<IOptions<OpenIddictValidationOptions>>().Value;
        validationOptions.Handlers.Should().Contain(h =>
            h.ServiceDescriptor != null &&
            (h.ServiceDescriptor.ServiceType == typeof(OpenIddictApplicationValidationHandler) ||
             h.ServiceDescriptor.ImplementationType == typeof(OpenIddictApplicationValidationHandler)));
    }
}
