using Xunit;
using System.Linq;

namespace PlayniteExtensions.Common.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("NIHON CREATE CO,LTD.", "NIHON CREATE")]
    [InlineData("Softstar Technology (Beijing) Co.,Ltd", "Softstar Technology (Beijing)")]
    public void TrimCompanyForms(string company, string expectedOutput)
    {
        var output = company.TrimCompanyForms();
        Assert.Equal(expectedOutput, output);
    }

    [Theory]
    [InlineData("38,9_Degrees")]
    public void SplitCompanies_Unchanged(string company)
    {
        var output = company.SplitCompanies();
        Assert.Single(output, company);
    }

    [Theory]
    [InlineData("Q Entertainment, SCE Studios Japan", "Q Entertainment", "SCE Studios Japan")]
    [InlineData("XSEED Games, Marvelous USA, Inc.", "XSEED Games", "Marvelous USA")]
    [InlineData("Simon & Schuster, Inc.", "Simon & Schuster")]
    public void SplitCompanies(string input, params string[] expectedCompanies)
    {
        var output = input.SplitCompanies().ToList();
        Assert.Equal(expectedCompanies.Length, output.Count);
        foreach (var company in expectedCompanies)
            Assert.True(output.Contains(company), $"||{string.Join(", ", output)}|| doesn't contain expected company {company}");
    }
}
