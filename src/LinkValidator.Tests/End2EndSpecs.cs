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

public class End2EndSpecs : TestKit, IClassFixture<TestWebServerFixture>
{
    private readonly TestWebServerFixture _webServerFixture;
    private readonly ITestOutputHelper _output;

    public End2EndSpecs(ITestOutputHelper output, TestWebServerFixture webServerFixture) : base(output: output)
    {
        _webServerFixture = webServerFixture;
        _output = output;
        
        _webServerFixture.Logger = _output.WriteLine;
        _webServerFixture.StartServer(RootPagePath);
    }
    
    public static readonly string RootPagePath = Path.Join(Directory.GetCurrentDirectory(), "pages");

    [Fact]
    public async Task ShouldCrawlWebsiteCorrectly()
    {
        // sanity check / pre-condition
        _output.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
        _output.WriteLine($"RootPagePath: {RootPagePath}");
        _output.WriteLine($"Full RootPagePath: {Path.GetFullPath(RootPagePath)}");
        
        Assert.True(Directory.Exists(RootPagePath));
        Assert.True(File.Exists(Path.Join(RootPagePath, "index.html")));
        
        // arrange - start test web server  
        var baseUrl = new AbsoluteUri(new Uri(_webServerFixture.BaseUrl!));
        
        // act
        var crawlResult = await CrawlWebsite(Sys, baseUrl);
        var markdown = GenerateMarkdown(baseUrl, crawlResult);
        
        // assert
        await Verify(markdown);
    }
}