using FluentAssertions;
using OpenIddict.Management.Options;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class OpenIddictManagementOptionsTests
{
    [Fact]
    public void CanInstantiateOptions()
    {
        var options = new OpenIddictManagementOptions();
        options.Should().NotBeNull();
        options.EnableAuditLogging.Should().BeTrue();
    }

    [Fact]
    public void CanToggle_EnableAuditLogging()
    {
        var options = new OpenIddictManagementOptions();
        options.EnableAuditLogging = false;
        options.EnableAuditLogging.Should().BeFalse();

        options.EnableAuditLogging = true;
        options.EnableAuditLogging.Should().BeTrue();
    }
}
