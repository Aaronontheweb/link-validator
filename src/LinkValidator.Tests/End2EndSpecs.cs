// -----------------------------------------------------------------------
// <copyright file="End2EndSpecs.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using Akka.TestKit.Xunit2;
using LinkValidator.Actors;
using Xunit.Abstractions;
using static LinkValidator.Util.CrawlerHelper;
using static LinkValidator.Util.MarkdownHelper;

namespace LinkValidator.Tests;

public class End2EndSpecs : TestKit
{
    public End2EndSpecs(ITestOutputHelper output) : base(output: output)
    {
    }
    
    public static readonly string RootPagePath = Path.Join(Directory.GetCurrentDirectory(), "pages");

    [Fact]
    public async Task ShouldCrawlWebsiteCorrectly()
    {
        // sanity check / pre-condition
        Assert.True(Directory.Exists(RootPagePath));
        Assert.True(File.Exists(Path.Join(RootPagePath, "index.html")));
        
        // need to start a process that will serve this content via a localhost address
        
        // arrange
        var baseUrl = new AbsoluteUri(new Uri(RootPagePath));
        
        // act
        var crawlResult = await CrawlWebsite(Sys, baseUrl);
        var markdown = GenerateMarkdown(baseUrl, crawlResult);
        
        // assert
        await Verify(markdown);
    }
}