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
    }
}
