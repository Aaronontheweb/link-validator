// -----------------------------------------------------------------------
// <copyright file="QueryStringPreservationSpecs.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using Akka.TestKit.Xunit2;
using LinkValidator.Actors;
using Xunit.Abstractions;
using static LinkValidator.Util.CrawlerHelper;

namespace LinkValidator.Tests;

/// <summary>
/// End-to-end coverage for issue #190: a query-bearing internal link must have its
/// query string transmitted in the actual HTTP request (not stripped during URL
/// normalization). This is the crawl-layer contract that the unit tests on
/// <see cref="UriHelpers.RemoveFragment"/> alone do not cover.
/// </summary>
public class QueryStringPreservationSpecs : TestKit
{
    private readonly TestWebServerFixture _webServerFixture;
    private readonly ITestOutputHelper _output;

    public QueryStringPreservationSpecs(ITestOutputHelper output) : base(output: output)
    {
        _webServerFixture = new TestWebServerFixture();
        _output = output;

        _webServerFixture.Logger = _output.WriteLine;
        // Dedicated port so this spec does not collide with the shared WebServer fixture.
        _webServerFixture.StartServer(
            Path.Join(Directory.GetCurrentDirectory(), "pages-query"),
            port: 8083);
    }

    [Fact]
    public async Task QueryString_should_be_transmitted_in_the_http_request()
    {
        // arrange - crawls the root page which links to /target.html?list=abc123
        var baseUrl = new AbsoluteUri(new Uri(_webServerFixture.BaseUrl!));

        // act - run a full crawl of the site
        var crawlResult = await CrawlWebsite(Sys, baseUrl);

        // assert - the target page was crawled and found WITH its query preserved,
        // proving the query survived normalization and reached the wire. If the
        // query had been stripped, the crawl would have looked up a different URL
        // (or none), and this assertion would fail.
        Assert.Contains(crawlResult.InternalLinks.Keys, k => k.Contains("target.html?list=abc123"));
    }
}
