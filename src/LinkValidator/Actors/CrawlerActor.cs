// -----------------------------------------------------------------------
// <copyright file="CrawlerActor.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Net;
using Akka.Actor;
using System.Net.Http;
using Akka.Event;
using LinkValidator.Util;
using static LinkValidator.Util.ParseHelpers;

namespace LinkValidator.Actors;

public interface ICrawlResult
{
    AbsoluteUri Url { get; }
    HttpStatusCode StatusCode { get; }
}

public record CrawlUrl(AbsoluteUri Url, LinkType LinkType);

public record PageCrawled(
    AbsoluteUri Url,
    HttpStatusCode StatusCode,
    IReadOnlyList<AbsoluteUri> InternalLinks,
    IReadOnlyList<AbsoluteUri> ExternalLinks) : ICrawlResult;

/// <summary>
/// Indicates whether an external link was found to be valid.
/// </summary>
/// <param name="Url">The link Uri</param>
/// <param name="StatusCode">The crawler status code</param>
public record ExternalLinkCrawled(AbsoluteUri Url, HttpStatusCode StatusCode) : ICrawlResult;

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
                { "User-Agent", $"LinkValidator/{typeof(CrawlerActor).Assembly.GetName().Version}" }
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
                if (_inflightRequests == _crawlConfiguration.MaxInflightRequests)
                    Become(TooBusy);
                break;
            case ICrawlResult pageCrawled:
                HandleCrawlResult(pageCrawled);
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
            case ICrawlResult pageCrawled:
                HandleCrawlResult(pageCrawled);
                // switch behaviors back and unstash one message
                Stash.Unstash();
                Become(OnReceive);
                break;
        }
    }

    private void HandleCrawlResult(ICrawlResult pageCrawled)
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
        switch (msg.LinkType)
        {
            case LinkType.Internal:
                CrawlInternalPage().PipeTo(Self, Self, result => result);
                break;
            case LinkType.External:
                CrawlExternalPage().PipeTo(Self, Self, result => result);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
       
        return;

        async Task<ICrawlResult> CrawlExternalPage()
        {
            try{
                using var cts = new CancellationTokenSource(_crawlConfiguration.RequestTimeout);
                var response = await _httpClient.GetAsync(msg.Url.Value, cts.Token);
                
                return new ExternalLinkCrawled(msg.Url, response.StatusCode);
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Failed to crawl {0}", msg.Url);
                return new ExternalLinkCrawled(msg.Url, HttpStatusCode.RequestTimeout);
            }
        }

        async Task<ICrawlResult> CrawlInternalPage()
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
                    var processingUri = UriHelpers.GetDirectoryPath(msg.Url);
                    var links = ParseLinks(html, processingUri);
                    
                    var internalLinks = links.Where(c => c.type == LinkType.Internal).Select(c => c.uri).ToImmutableArray();
                    var externalLinks = links.Where(c => c.type == LinkType.External).Select(c => c.uri).ToImmutableArray();

                    return new PageCrawled(msg.Url, response.StatusCode, internalLinks, externalLinks);
                }

                return new PageCrawled(msg.Url, response.StatusCode, [], []);
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Failed to crawl {0}", msg.Url);
                return new PageCrawled(msg.Url, HttpStatusCode.RequestTimeout, [], []);
            }
        }
    }

    public IStash Stash { get; set; } = null!;

    protected override void PostStop()
    {
        _httpClient.Dispose();
    }
}