using Firma.Connect.Api.Features.Institutions;

namespace Firma.Connect.Api.Tests;

public class InstitutionDirectoryServiceTests
{
    [Theory]
    [InlineData("  Universidade de São Paulo  ", "universidade de sao paulo")]
    [InlineData("FUNEPE", "funepe")]
    [InlineData("Faculdade José do Brasil", "faculdade jose do brasil")]
    public void Normalize_removes_accents_spaces_and_case(string value, string expected)
    {
        Assert.Equal(expected, InstitutionDirectoryService.Normalize(value));
    }
}
