// -----------------------------------------------------------------------
// <copyright file="CrawlReportSpecs.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Net;
using LinkValidator.Actors;
using LinkValidator.Util;

namespace LinkValidator.Tests;

public class CrawlReportSpecs
{
    private static readonly AbsoluteUri Root = new(new Uri("http://localhost:8080/"));

    private static CrawlRecord Ok(string path) =>
        new(new AbsoluteUri(new Uri($"http://localhost:8080{path}")), HttpStatusCode.OK,
            ImmutableList<AbsoluteUri>.Empty);

    private static CrawlRecord NotFound(string path) =>
        new(new AbsoluteUri(new Uri($"http://localhost:8080{path}")), HttpStatusCode.NotFound,
            ImmutableList<AbsoluteUri>.Empty);

    [Fact]
    public void HasStrictCrawlErrors_ShouldBeFalse_WhenAllLinksAreOk()
    {
        var report = new CrawlReport(
            Root,
            ImmutableSortedDictionary<string, CrawlRecord>.Empty
                .Add("/", Ok("/"))
                .Add("/page1.html", Ok("/page1.html")),
            ImmutableSortedDictionary<string, CrawlRecord>.Empty);

        Assert.False(report.HasStrictCrawlErrors);
    }

    [Fact]
    public void HasStrictCrawlErrors_ShouldBeTrue_WhenAnInternalLinkIsBroken()
    {
        var report = new CrawlReport(
            Root,
            ImmutableSortedDictionary<string, CrawlRecord>.Empty
                .Add("/", Ok("/"))
                .Add("/page2.html", NotFound("/page2.html")),
            ImmutableSortedDictionary<string, CrawlRecord>.Empty);

        Assert.True(report.HasStrictCrawlErrors);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.BadGateway)]
    public void HasStrictCrawlErrors_ShouldIgnoreIndeterminateExternalFailures(HttpStatusCode statusCode)
    {
        var report = new CrawlReport(
            Root,
            ImmutableSortedDictionary<string, CrawlRecord>.Empty
                .Add("/", Ok("/")),
            ImmutableSortedDictionary<string, CrawlRecord>.Empty
                .Add("https://twitter.com/someprofile",
                    new CrawlRecord(new AbsoluteUri(new Uri("https://twitter.com/someprofile")),
                        statusCode, ImmutableList<AbsoluteUri>.Empty)));

        Assert.False(report.HasStrictCrawlErrors);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Gone)]
    public void HasStrictCrawlErrors_ShouldBeTrue_WhenAnExternalLinkIsPermanentlyMissing(HttpStatusCode statusCode)
    {
        var report = new CrawlReport(
            Root,
            ImmutableSortedDictionary<string, CrawlRecord>.Empty
                .Add("/", Ok("/")),
            ImmutableSortedDictionary<string, CrawlRecord>.Empty
                .Add("https://example.com/missing",
                    new CrawlRecord(new AbsoluteUri(new Uri("https://example.com/missing")),
                        statusCode, ImmutableList<AbsoluteUri>.Empty)));

        Assert.True(report.HasStrictCrawlErrors);
    }
}
