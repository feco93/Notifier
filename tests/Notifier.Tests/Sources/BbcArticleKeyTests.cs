using Notifier.Sources.Bbc;

namespace Notifier.Tests.Sources;

public sealed class BbcArticleKeyTests
{
    [Theory]
    [InlineData("https://www.bbc.co.uk/news/articles/c6n90g2l82d1o?at_medium=RSS&at_campaign=rss", "c6n90g2l82d1o")]
    [InlineData("https://www.bbc.co.uk/news/articles/c6n90g2l82d1o#3", "c6n90g2l82d1o")]
    [InlineData("https://www.bbc.co.uk/news/videos/ckedq2edqegzo#1", "ckedq2edqegzo")]
    [InlineData("https://www.bbc.co.uk/weather/articles/c5742nplemnno", "c5742nplemnno")]
    [InlineData("https://www.bbc.co.uk/news/articles/c6n90g2l82d1o/", "c6n90g2l82d1o")]
    public void Uses_last_path_segment(string url, string expected)
    {
        Assert.Equal(expected, BbcArticleKey.From(url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("https://www.bbc.co.uk/")]
    public void Returns_null_when_no_key_can_be_derived(string? url)
    {
        Assert.Null(BbcArticleKey.From(url));
    }
}
