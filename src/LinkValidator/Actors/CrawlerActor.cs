using System.Collections.Immutable;
using System.Net;
using Akka.Actor;
using System.Net.Http;
using Akka.Event;
using static LinkValidator.Util.ParseHelpers;

namespace LinkValidator.Actors;

public record CrawlUrl(string Url);
public record PageCrawled(string Url, HttpStatusCode StatusCode, IReadOnlyList<string> Links);
public record CrawlComplete(ImmutableDictionary<string, (HttpStatusCode StatusCode, string Path)> Results);

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




    public IStash Stash { get; set; } = null!;
}