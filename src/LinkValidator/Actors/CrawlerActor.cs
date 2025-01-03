using System.Collections.Immutable;
using System.Net;
using Akka.Actor;
using System.Net.Http;
using Akka.Event;
using HtmlAgilityPack;
using static LinkValidator.Util.UriHelpers;

namespace LinkValidator.Actors;

public record CrawlUrl(string Url);
public record PageCrawled(string Url, HttpStatusCode StatusCode, IReadOnlyList<string> Links);
public record CrawlComplete(ImmutableDictionary<string, (HttpStatusCode StatusCode, string Path)> Results);

/// <summary>
/// Configuration for the crawler
/// </summary>
/// <param name="BaseUrl">The base url - we are only interested in urls stemming from it.</param>
/// <param name="MaxInflightRequests">Max degree of parallelism.</param>
/// <param name="RequestTimeout">The amount of time we'll allot for any individual HTTP request</param>
public sealed record CrawlConfiguration(string BaseUrl, int MaxInflightRequests, TimeSpan RequestTimeout);

public sealed class CrawlerActor : UntypedActor, IWithStash
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly CrawlConfiguration _crawlConfiguration;
    private readonly IActorRef _coordinator;
    private readonly IHttpClientFactory _httpClientFactory;
    
    private int _inflightRequests = 0;

    public CrawlerActor(IHttpClientFactory httpClientFactory, CrawlConfiguration crawlConfiguration, IActorRef coordinator)
    {
        _httpClientFactory = httpClientFactory;
        _crawlConfiguration = crawlConfiguration;
        _coordinator = coordinator;
    }

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case CrawlUrl crawlUrl:
                HandleCrawlUrl(crawlUrl);
                
                // switch behaviors to "waiting" if we've hit our max inflight requests
                if(_inflightRequests == _crawlConfiguration.MaxInflightRequests)
                    Become(TooBusy);
                break;
            case PageCrawled pageCrawled:
                HandlePageCrawled(pageCrawled);
                break;
        }
    }

    private void TooBusy(object message)
    {
        switch (message)
        {
            case CrawlUrl:
                // too many in-flight requests right now
                Stash.Stash();
                break;
            case PageCrawled pageCrawled:
                HandlePageCrawled(pageCrawled);
                
                // switch behaviors back and unstash one message
                Stash.Unstash();
                Become(OnReceive);
                break;
        }
    }

    private void HandlePageCrawled(PageCrawled pageCrawled)
    {
        _inflightRequests--;
        _coordinator.Tell(pageCrawled);
    }

    private void HandleCrawlUrl(CrawlUrl msg)
    {
        /*
         * We will not receive a CrawlUrl message from the IndexerActor if we've
         * already seen this page before.
         */
        _inflightRequests++;

        async Task<PageCrawled> DoWork()
        {
            try
            {
                using var cts = new CancellationTokenSource(_crawlConfiguration.RequestTimeout);
                using var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(msg.Url, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var html = await response.Content.ReadAsStringAsync(cts.Token);
                    var links = ParseLinks(html, _crawlConfiguration.BaseUrl);

                    return new PageCrawled(msg.Url, response.StatusCode, links);
                }

                return new PageCrawled(msg.Url, response.StatusCode, Array.Empty<string>());
            }
            catch(Exception ex)
            {
                _log.Warning(ex, "Failed to crawl {0}", msg.Url);
                return new PageCrawled(msg.Url, HttpStatusCode.RequestTimeout, Array.Empty<string>());
            }
        }
        
        DoWork().PipeTo(Self, Self, result => result);
    }

    private static IReadOnlyList<string> ParseLinks(string html, string baseUrl)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        IReadOnlyList<string> links = doc.DocumentNode
            .SelectNodes("//a[@href]")?
            .Select(node => node.GetAttributeValue("href", ""))
            .Where(href => !string.IsNullOrEmpty(href))
            .Select(href => NormalizeUrl(baseUrl, href))
            .Where(href => href.StartsWith(baseUrl))
            .ToArray() ?? [];
        return links;
    }


    public IStash Stash { get; set; } = null!;
}