using Seq.Input.HealthCheck.Util;
using Xunit;

namespace Seq.Input.HealthCheck.Tests.Util;

public class UriHelperTests
{
    [Theory]
    [InlineData("https://example.com", "https://example.com?q=42")]
    [InlineData("https://example.com/p", "https://example.com/p?q=42")]
    [InlineData("https://example.com/p/", "https://example.com/p/?q=42")]
    [InlineData("https://example.com?a", "https://example.com?a&q=42")]
    [InlineData("https://example.com?a=1", "https://example.com?a=1&q=42")]
    public void ParametersAreAppendedCorrectly(string initial, string expected)
    {
        var actual = UriHelper.AppendParameter(initial, "q", "42");
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("https://example.com", "https://www.example.com", null)]
    [InlineData("https://example.com", "https://example.datalust.co", null)]
    [InlineData("https://example.com", "https://example.com:1234/test?x=y", null)]
    [InlineData("https://example.com", "/test", "https://example.com/test")]
    [InlineData("https://example.com/health?x=y", "/test", "https://example.com/test")]
    public void LocationHeadersAreMadeAbsolute(string requestUri, string locationHeader, string? expected)
    {
        var actual = UriHelper.MakeAbsoluteLocation(requestUri, locationHeader);
        Assert.Equal(expected ?? locationHeader, actual);
    }
}