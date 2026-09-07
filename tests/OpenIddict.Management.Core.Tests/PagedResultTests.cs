using FluentAssertions;
using OpenIddict.Management.Dto;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class PagedResultTests
{
    [Theory]
    [InlineData(10, 5, 2)]
    [InlineData(11, 5, 3)]
    [InlineData(0, 5, 0)]
    [InlineData(1, 10, 1)]
    public void TotalPages_CalculatesCorrectly(int totalCount, int pageSize, int expectedPages)
    {
        var result = new PagedResult<string>
        {
            Items = [],
            PageIndex = 1,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        result.TotalPages.Should().Be(expectedPages);
    }

    [Fact]
    public void HasPreviousPage_And_HasNextPage_ComputeCorrectly()
    {
        var page1 = new PagedResult<string>
        {
            Items = ["a", "b"],
            PageIndex = 1,
            PageSize = 2,
            TotalCount = 5
        };

        page1.HasPreviousPage.Should().BeFalse();
        page1.HasNextPage.Should().BeTrue();

        var page3 = new PagedResult<string>
        {
            Items = ["e"],
            PageIndex = 3,
            PageSize = 2,
            TotalCount = 5
        };

        page3.HasPreviousPage.Should().BeTrue();
        page3.HasNextPage.Should().BeFalse();
    }
}
