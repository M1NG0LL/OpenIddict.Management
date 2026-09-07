using FluentAssertions;
using OpenIddict.Management.Dashboard;
using OpenIddict.Management.Enums;
using Xunit;

namespace OpenIddict.Management.Dashboard.Tests;

public class DashboardOptionsTests
{
    [Fact]
    public void DefaultOptions_ShouldHaveExpectedDefaults()
    {
        var options = new DashboardOptions();

        options.PathPrefix.Should().Be("/management");
        options.DashboardTitle.Should().Be("OpenIddict Management");
        options.AuthorizationPolicy.Should().BeNull();
        options.RequireAuthorization.Should().BeTrue();
        options.EnabledFeatures.Should().Be(DashboardFeature.All);
    }

    [Fact]
    public void Options_PropertiesCanBeModified()
    {
        var options = new DashboardOptions
        {
            PathPrefix = "/admin/identity",
            DashboardTitle = "Custom Admin Console",
            AuthorizationPolicy = "RequireAdminRole",
            RequireAuthorization = false,
            EnabledFeatures = DashboardFeature.ApplicationManagement | DashboardFeature.TokenInspector
        };

        options.PathPrefix.Should().Be("/admin/identity");
        options.DashboardTitle.Should().Be("Custom Admin Console");
        options.AuthorizationPolicy.Should().Be("RequireAdminRole");
        options.RequireAuthorization.Should().BeFalse();
        options.EnabledFeatures.Should().Be(DashboardFeature.ApplicationManagement | DashboardFeature.TokenInspector);
    }
}
