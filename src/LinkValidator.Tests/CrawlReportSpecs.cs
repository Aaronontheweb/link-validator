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
    public void HasInternalCrawlErrors_ShouldBeFalse_WhenAllInternalLinksOk()
    {
        var report = new CrawlReport(
            Root,
            ImmutableSortedDictionary<string, CrawlRecord>.Empty
                .Add("/", Ok("/"))
                .Add("/page1.html", Ok("/page1.html")),
            ImmutableSortedDictionary<string, CrawlRecord>.Empty);

        Assert.False(report.HasInternalCrawlErrors);
    }

    [Fact]
    public void HasInternalCrawlErrors_ShouldBeTrue_WhenAnInternalLinkIsBroken()
    {
        var report = new CrawlReport(
            Root,
            ImmutableSortedDictionary<string, CrawlRecord>.Empty
                .Add("/", Ok("/"))
                .Add("/page2.html", NotFound("/page2.html")),
            ImmutableSortedDictionary<string, CrawlRecord>.Empty);

        Assert.True(report.HasInternalCrawlErrors);
    }

    [Fact]
    public void HasInternalCrawlErrors_ShouldIgnoreExternalLinkFailures()
    {
        // External link failures must not fail a --strict run (e.g. X/Twitter returns 520 to crawlers).
        var report = new CrawlReport(
            Root,
            ImmutableSortedDictionary<string, CrawlRecord>.Empty
                .Add("/", Ok("/")),
            ImmutableSortedDictionary<string, CrawlRecord>.Empty
                .Add("https://twitter.com/someprofile",
                    new CrawlRecord(new AbsoluteUri(new Uri("https://twitter.com/someprofile")),
                        HttpStatusCode.BadGateway, ImmutableList<AbsoluteUri>.Empty)));

        Assert.False(report.HasInternalCrawlErrors);
    }
}