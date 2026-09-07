using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Management.Dashboard;
using OpenIddict.Management.Dashboard.Middleware;
using Xunit;

namespace OpenIddict.Management.Dashboard.Tests;

public class DashboardMiddlewareTests
{
    private static DefaultHttpContext CreateHttpContext(ClaimsPrincipal user, string path)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services,
            User = user
        };
        context.Request.Path = path;
        return context;
    }

    [Fact]
    public async Task InvokeAsync_NonMatchingPath_ShouldCallNext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new DashboardMiddleware(next);
        var httpContext = CreateHttpContext(new ClaimsPrincipal(new ClaimsIdentity()), "/api/applications");
        var options = Microsoft.Extensions.Options.Options.Create(new DashboardOptions { PathPrefix = "/management" });

        // Act
        await middleware.InvokeAsync(httpContext, options);

        // Assert
        nextCalled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_MatchingPath_Unauthenticated_ShouldReturn401()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new DashboardMiddleware(next);
        var httpContext = CreateHttpContext(new ClaimsPrincipal(new ClaimsIdentity()), "/management");
        var options = Microsoft.Extensions.Options.Options.Create(new DashboardOptions
        {
            PathPrefix = "/management",
            RequireAuthorization = true,
            AuthorizationPolicy = null
        });

        // Act
        await middleware.InvokeAsync(httpContext, options);

        // Assert
        nextCalled.Should().BeFalse();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_MatchingPath_Authenticated_ShouldCallNext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new DashboardMiddleware(next);
        var identity = new ClaimsIdentity("TestAuth");
        var httpContext = CreateHttpContext(new ClaimsPrincipal(identity), "/management/Applications");

        var options = Microsoft.Extensions.Options.Options.Create(new DashboardOptions
        {
            PathPrefix = "/management",
            RequireAuthorization = true,
            AuthorizationPolicy = null
        });

        // Act
        await middleware.InvokeAsync(httpContext, options);

        // Assert
        nextCalled.Should().BeTrue();
    }
}
