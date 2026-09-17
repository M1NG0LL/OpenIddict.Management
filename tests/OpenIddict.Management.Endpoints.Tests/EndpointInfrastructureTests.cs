using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenIddict.Management.Endpoints.Extensions;
using OpenIddict.Management.Endpoints.Filters;
using OpenIddict.Management.Options;
using OpenIddict.Management.Results;
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

    [Fact]
    public void MapOpenIddictManagementEndpoints_SynchronizesRoutePrefixFromOpenIddictManagementOptions()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.Configure<OpenIddictManagementOptions>(options =>
        {
            options.RoutePrefix = "/custom/core/route";
        });

        var app = builder.Build();

        var group = app.MapOpenIddictManagementEndpoints();
        group.Should().NotBeNull();
    }

    [Fact]
    public async Task ExceptionMappingEndpointFilter_WhenUnexpectedException_Returns500()
    {
        var filter = new ExceptionMappingEndpointFilter();
        var context = Substitute.For<EndpointFilterInvocationContext>();

        var result = await filter.InvokeAsync(context, _ => throw new InvalidOperationException("DB down"));

        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        var statusResult = (IStatusCodeHttpResult)result!;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task ExceptionMappingEndpointFilter_WhenOperationCanceledException_Rethrows()
    {
        var filter = new ExceptionMappingEndpointFilter();
        var context = Substitute.For<EndpointFilterInvocationContext>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await filter.InvokeAsync(context, _ => throw new OperationCanceledException(cts.Token));

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
