using Firma.Connect.Api.Contracts;

namespace Firma.Connect.Api.Tests;

public class ProfileSearchQueryTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 0)]
    [InlineData(2, 20, 20)]
    public void Skip_is_calculated_from_page(int page, int pageSize, int expectedSkip)
    {
        var query = new ProfileSearchQuery(null, true, null, Page: page, PageSize: pageSize);
        var normalizedPage = Math.Max(query.Page, 1);

        Assert.Equal(expectedSkip, (normalizedPage - 1) * query.PageSize);
    }
}
