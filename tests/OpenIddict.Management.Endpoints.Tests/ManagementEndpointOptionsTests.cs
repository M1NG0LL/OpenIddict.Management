using FluentAssertions;
using OpenIddict.Management.Endpoints;
using Xunit;

namespace OpenIddict.Management.Endpoints.Tests;

public class ManagementEndpointOptionsTests
{
    [Fact]
    public void DefaultRoutePrefix_ShouldBeApiManagement()
    {
        var options = new ManagementEndpointOptions();
        options.RoutePrefix.Should().Be("/api/management");
    }
}
