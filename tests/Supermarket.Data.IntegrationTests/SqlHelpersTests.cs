namespace Supermarket.Data.IntegrationTests;

/// <summary>Fast tests for the pure helpers; they do not touch the database.</summary>
public class SqlHelpersTests
{
    [Theory]
    [InlineData("50%", "50[%]")]
    [InlineData("o_h", "o[_]h")]
    [InlineData("[A]", "[[]A]")]
    [InlineData("plain", "plain")]
    public void Like_wildcards_are_escaped(string input, string expected) =>
        Assert.Equal(expected, SearchTerm.EscapeLike(input));

    [Theory]
    [InlineData(" 42 ", 42)]
    [InlineData("-1", null)]
    [InlineData("4.5", null)]
    [InlineData("leche", null)]
    public void Numeric_terms_also_search_by_id(string input, int? expectedId) =>
        Assert.Equal(expectedId, SearchTerm.Parse(input).Id);

    [Fact]
    public void Scripts_are_split_on_go_separators()
    {
        const string Script = "SELECT 1;\nGO\n  go ;\nSELECT 2;\r\nGO\r\nSELECT 'GOOD';";

        Assert.Equal(["SELECT 1;", "SELECT 2;", "SELECT 'GOOD';"], DatabaseInitializer.SplitBatches(Script));
    }

    [Theory]
    [InlineData("schema.sql")]
    [InlineData("seed.sql")]
    public void Scripts_are_embedded_in_the_assembly(string name) =>
        Assert.NotEmpty(DatabaseInitializer.ReadScript(name));
}
