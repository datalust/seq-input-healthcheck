using Seq.Input.HealthCheck.Util;
using Xunit;

namespace Seq.Input.HealthCheck.Tests.Util;

public class UrlHelperTests
{
    [Theory]
    [InlineData("https://example.com", "https://example.com?q=42")]
    [InlineData("https://example.com/p", "https://example.com/p?q=42")]
    [InlineData("https://example.com/p/", "https://example.com/p/?q=42")]
    [InlineData("https://example.com?a", "https://example.com?a&q=42")]
    [InlineData("https://example.com?a=1", "https://example.com?a=1&q=42")]
    public void ParametersAreAppendedCorrectly(string initial, string expected)
    {
        var actual = UrlHelper.AppendParameter(initial, "q", "42");
        Assert.Equal(expected, actual);
    }
}