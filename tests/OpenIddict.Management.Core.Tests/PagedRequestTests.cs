using FluentAssertions;
using OpenIddict.Management.Constants;
using OpenIddict.Management.Dto;
using OpenIddict.Management.Enums;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class PagedRequestTests
{
    [Fact]
    public void WithSort_Enum_SetsSortByAndSortDescending()
    {
        var request = new PagedRequest()
            .WithSort(ApplicationSortField.DisplayName, sortDescending: true);

        request.SortBy.Should().Be("DisplayName");
        request.SortBy.Should().Be(ApplicationSortProperties.DisplayName);
        request.SortDescending.Should().BeTrue();
    }
}
