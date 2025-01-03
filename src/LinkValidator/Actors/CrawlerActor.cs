using System.Collections.Immutable;
using System.Net;
using Akka.Actor;
using System.Net.Http;
using Akka.Event;
using static LinkValidator.Util.ParseHelpers;

namespace LinkValidator.Actors;

public record CrawlUrl(AbsoluteUri Url);
public record PageCrawled(AbsoluteUri Url, HttpStatusCode StatusCode, IReadOnlyList<AbsoluteUri> Links);

public sealed class CrawlerActor : UntypedActor, IWithStash
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly CrawlConfiguration _crawlConfiguration;
    private readonly IActorRef _coordinator;
    private readonly HttpClient _httpClient;
    
    private int _inflightRequests = 0;

    public CrawlerActor(CrawlConfiguration crawlConfiguration, IActorRef coordinator)
    {
        _httpClient = new HttpClient()
        {
            DefaultRequestHeaders =
            {
                {"User-Agent", $"LinkValidator/{typeof(CrawlerActor).Assembly.GetName().Version}"}
            }
        };
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

        DoWork().PipeTo(Self, Self, result => result);
        return;

        async Task<PageCrawled> DoWork()
        {
            try
            {
                using var cts = new CancellationTokenSource(_crawlConfiguration.RequestTimeout);
                var response = await _httpClient.GetAsync(msg.Url.Value, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var html = await response.Content.ReadAsStringAsync(cts.Token);
                    /*
                     * A subtle but important note: we pass in THE CURRENT URL WE ARE QUERYING here
                     * rather than the configured base url. Why might that be?
                     *
                     * This is because:
                     *
                     * 1. We know, with certainty, that this URL is valid AND belongs to our target domain.
                     * 2. If we need to resolve relative urls, i.e. "../about", we need to know the current path
                     * in order to do that. Preserving the current URL allows us to do that.
                     */
                    var processingUri = new Uri(msg.Url.Value, ".");
                    var links = ParseLinks(html, new AbsoluteUri(processingUri));

                    return new PageCrawled(msg.Url, response.StatusCode, links);
                }

                return new PageCrawled(msg.Url, response.StatusCode, Array.Empty<AbsoluteUri>());
            }
            catch(Exception ex)
            {
                _log.Warning(ex, "Failed to crawl {0}", msg.Url);
                return new PageCrawled(msg.Url, HttpStatusCode.RequestTimeout, Array.Empty<AbsoluteUri>());
            }
        }
    }

    public IStash Stash { get; set; } = null!;

    protected override void PostStop()
    {
        _httpClient.Dispose();
    }
}