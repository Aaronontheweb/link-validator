using System.Collections.Immutable;
using System.Net;
using LinkValidator.Actors;
using LinkValidator.Util;

namespace LinkValidator.Tests;

public class ManualMarkdownTest
{
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

        var markdown = MarkdownHelper.GenerateMarkdown(baseUri, results);
        
        Console.WriteLine("RAW MARKDOWN:");
        Console.WriteLine(markdown);
        Console.WriteLine("END RAW MARKDOWN");
        
        // Check for escaping
        Assert.DoesNotContain("\\/", markdown);
        Assert.Contains("/page2.html", markdown);
        Assert.Contains("NotFound", markdown);
    }
}