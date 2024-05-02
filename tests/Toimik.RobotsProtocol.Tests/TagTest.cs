namespace Toimik.RobotsProtocol.Tests;

using Xunit;

public class TagTest
{
    [Theory]
    [InlineData("all", null, null, "all")]
    [InlineData("all", null, "bot", "bot: all")]
    [InlineData("max-snippet", "100", null, "max-snippet: 100")]
    [InlineData("max-snippet", "100", "bot", "bot: max-snippet: 100")]
    public void InstantiateWithNullUserAgent(
        string directive,
        string? value,
        string? userAgent,
        string expectedText)
    {
        var tag = new Tag(
            directive: directive,
            value: value,
            userAgent: userAgent);

        var actualText = tag.ToString();
        Assert.Equal(expectedText, actualText);
    }
}