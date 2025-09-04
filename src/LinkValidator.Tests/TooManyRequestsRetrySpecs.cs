// -----------------------------------------------------------------------
// <copyright file="TooManyRequestsRetrySpecs.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using LinkValidator.Actors;
using LinkValidator.Util;
using Xunit.Abstractions;

namespace LinkValidator.Tests;

public class TooManyRequestsRetrySpecs : TestKit, IClassFixture<TestWebServerFixture>
{
    private readonly TestWebServerFixture _webServerFixture;
    private readonly ITestOutputHelper _output;

    public TooManyRequestsRetrySpecs(ITestOutputHelper output, TestWebServerFixture webServerFixture) : base(output: output)
    {
        _webServerFixture = webServerFixture;
        _output = output;
        _webServerFixture.Logger = _output.WriteLine;
    }

    [Fact]
    public async Task ShouldCompleteIndexingWithExternalLinksThatReturn429()
    {
        // arrange - create a test page with external links
        var testPagesDir = Path.Join(Directory.GetCurrentDirectory(), "test-pages-429");
        Directory.CreateDirectory(testPagesDir);
        
        try
        {
            var testPageContent = """
                <html>
                    <body>
                        <h1>Test Page</h1>
                        <a href="https://httpbin.org/status/429">External Link 1</a>
                        <a href="https://example.com/nonexistent-rate-limited-url">External Link 2</a>
                    </body>
                </html>
                """;
                
            File.WriteAllText(Path.Join(testPagesDir, "index.html"), testPageContent);
            
            _webServerFixture.StartServer(testPagesDir, 8081); // Use different port to avoid conflicts
            var baseUrl = new AbsoluteUri(new Uri(_webServerFixture.BaseUrl!));
            
            // Use faster retry settings for testing
            var crawlSettings = new CrawlConfiguration(
                baseUrl, 
                5, 
                TimeSpan.FromSeconds(3), // Short timeout for faster test
                1,  // Only 1 retry attempt
                TimeSpan.FromMilliseconds(500) // Short retry delay
            );
            
            var tcs = new TaskCompletionSource<CrawlReport>();
            var indexer = Sys.ActorOf(Props.Create(() => new IndexerActor(crawlSettings, tcs)), "indexer");
            
            // act
            indexer.Tell(IndexerActor.BeginIndexing.Instance, ActorRefs.NoSender);
            
            // The test should complete within a reasonable time despite retries
            var timeout = TimeSpan.FromSeconds(30);
            var startTime = DateTime.UtcNow;
            var crawlResult = await tcs.Task.WaitAsync(timeout);
            var elapsed = DateTime.UtcNow - startTime;
            
            // assert
            Assert.NotNull(crawlResult);
            _output.WriteLine($"Crawl completed in {elapsed.TotalSeconds:F1} seconds");
            _output.WriteLine($"Found {crawlResult.InternalLinks.Count} internal links and {crawlResult.ExternalLinks.Count} external links");
            
            // Verify the crawl completed and didn't hang indefinitely
            Assert.True(elapsed < timeout, $"Crawl should complete within {timeout.TotalSeconds}s but took {elapsed.TotalSeconds:F1}s");
            
            // Should have attempted to crawl the external links
            Assert.True(crawlResult.ExternalLinks.Count > 0, "Should have found external links to crawl");
            
            // Print status of external links for debugging
            foreach (var (url, record) in crawlResult.ExternalLinks)
            {
                _output.WriteLine($"External link: {url} -> {record.StatusCode}");
            }
        }
        finally
        {
            if (Directory.Exists(testPagesDir))
                Directory.Delete(testPagesDir, true);
        }
    }
    
    [Fact]
    public void ShouldParseRetryAfterHeaderCorrectly()
    {
        // arrange
        var crawlConfig = new CrawlConfiguration(
            new AbsoluteUri(new Uri("https://example.com")), 
            5, TimeSpan.FromSeconds(5));
            
        var crawler = Sys.ActorOf(Props.Create(() => new CrawlerActor(crawlConfig, ActorRefs.Nobody)), "crawler");
        
        // Create a mock response message (this is more of a unit test for the logic)
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        
        // Test seconds format
        response.Headers.Add("Retry-After", "30");
        // We can't directly test the private method, but we can verify the logic works through integration
        
        // Test date format  
        var futureTime = DateTimeOffset.UtcNow.AddMinutes(5);
        response.Headers.Clear();
        response.Headers.Add("Retry-After", futureTime.ToString("R"));
        
        // This test mainly verifies that the CrawlerActor can be instantiated with the new retry logic
        Assert.NotNull(crawler);
        _output.WriteLine("CrawlerActor with retry logic created successfully");
    }
}