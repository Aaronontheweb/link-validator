using System.Collections.Immutable;
using System.Net;
using LinkValidator.Actors;
using LinkValidator.Util;
using Xunit.Abstractions;

namespace LinkValidator.Tests;

public class ManualMarkdownTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ManualMarkdownTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void TestRawMarkdownOutput()
    {
        // Create sample data
        var baseUri = new AbsoluteUri(new Uri("http://localhost:8080"));
        var results = ImmutableSortedDictionary.Create<string, CrawlRecord>()
            .Add("/", new CrawlRecord(baseUri, HttpStatusCode.OK, ImmutableList<AbsoluteUri>.Empty))
            .Add("/page2.html", new CrawlRecord(
                new AbsoluteUri(new Uri("http://localhost:8080/page2.html")), 
                HttpStatusCode.NotFound, 
                ImmutableList<AbsoluteUri>.Empty
                    .Add(baseUri)
                    .Add(new AbsoluteUri(new Uri("http://localhost:8080/index.html")))));
        
        var crawlResults = new CrawlReport(baseUri, results, ImmutableSortedDictionary<string, CrawlRecord>.Empty);

        var markdown = MarkdownHelper.GenerateMarkdown(crawlResults);
        
        _testOutputHelper.WriteLine("RAW MARKDOWN:");
        _testOutputHelper.WriteLine(markdown);
        _testOutputHelper.WriteLine("END RAW MARKDOWN");
        
        // Check for escaping
        Assert.DoesNotContain("\\/", markdown);
        Assert.Contains("/page2.html", markdown);
        Assert.Contains("NotFound", markdown);
    }
}