using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Management.Endpoints.Extensions;
using Xunit;

namespace OpenIddict.Management.Endpoints.Tests;

public class EndpointInfrastructureTests
{
    [Fact]
    public void MapOpenIddictManagementEndpoints_ReturnsNonNullGroupBuilder()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // Act
        var group = app.MapOpenIddictManagementEndpoints(options =>
        {
            options.RoutePrefix = "/custom/management/api";
        });

        // Assert
        group.Should().NotBeNull();
    }
}
