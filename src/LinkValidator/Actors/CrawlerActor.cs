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

public record RetryExternalRequest(AbsoluteUri Url, int RetryCount);

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

public record ExternalLinkRetryScheduled(AbsoluteUri Url, HttpStatusCode StatusCode) : ICrawlResult;

public sealed class CrawlerActor : UntypedActor, IWithStash
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly CrawlConfiguration _crawlConfiguration;
    private readonly IActorRef _coordinator;
    private readonly HttpClient _httpClient;
    private readonly Random _random = new();

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
            case RetryExternalRequest retryRequest:
                HandleRetryExternalRequest(retryRequest);
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
            case RetryExternalRequest retryRequest:
                HandleRetryExternalRequest(retryRequest);
                break;
        }
    }

    private void HandleCrawlResult(ICrawlResult pageCrawled)
    {
        _inflightRequests--;
        _coordinator.Tell(pageCrawled);
    }

    private void HandleRetryExternalRequest(RetryExternalRequest retryRequest)
    {
        if (retryRequest.RetryCount > _crawlConfiguration.MaxExternalRetries)
        {
            _log.Warning("Max retries ({0}) exceeded for external URL {1}", _crawlConfiguration.MaxExternalRetries, retryRequest.Url);
            _coordinator.Tell(new ExternalLinkCrawled(retryRequest.Url, HttpStatusCode.TooManyRequests));
            return;
        }

        _log.Info("Retrying external request for {0} (attempt {1})", retryRequest.Url, retryRequest.RetryCount);
        
        CrawlExternalPageInternal(retryRequest.Url, retryRequest.RetryCount).PipeTo(Self, Self, result => result);
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
                CrawlExternalPageInternal(msg.Url, 0).PipeTo(Self, Self, result => result);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
       
        return;

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

    private async Task<ICrawlResult> CrawlExternalPageInternal(AbsoluteUri url, int retryCount)
    {
        // Capture context-dependent references before async operation
        var scheduler = Context.System.Scheduler;
        var self = Self;
        
        try
        {
            using var cts = new CancellationTokenSource(_crawlConfiguration.RequestTimeout);
            var response = await _httpClient.GetAsync(url.Value, cts.Token);
            
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (retryCount >= _crawlConfiguration.MaxExternalRetries)
                {
                    _log.Warning("Max retries ({0}) exceeded for external URL {1}", 
                        _crawlConfiguration.MaxExternalRetries, url);
                    return new ExternalLinkCrawled(url, HttpStatusCode.TooManyRequests);
                }
                
                var baseDelay = ParseRetryAfterHeader(response) ?? _crawlConfiguration.DefaultExternalRetryDelay;
                var jitteredDelay = AddJitter(baseDelay);
                _log.Warning("Received 429 TooManyRequests for {0} (retry {1}), scheduling retry in {2} (base: {3})", 
                    url, retryCount, jitteredDelay, baseDelay);
                
                scheduler.ScheduleTellOnce(jitteredDelay, self, new RetryExternalRequest(url, retryCount + 1), self);
                
                return new ExternalLinkRetryScheduled(url, HttpStatusCode.TooManyRequests);
            }
            
            return new ExternalLinkCrawled(url, response.StatusCode);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to crawl {0}", url);
            return new ExternalLinkCrawled(url, HttpStatusCode.RequestTimeout);
        }
    }

    private TimeSpan AddJitter(TimeSpan baseDelay)
    {
        // Add ±25% jitter to prevent thundering herd
        var jitterRange = baseDelay.TotalMilliseconds * 0.25;
        var jitterMs = _random.NextDouble() * jitterRange * 2 - jitterRange; // -25% to +25%
        var jitteredMs = Math.Max(100, baseDelay.TotalMilliseconds + jitterMs); // Minimum 100ms
        
        return TimeSpan.FromMilliseconds(jitteredMs);
    }

    private TimeSpan? ParseRetryAfterHeader(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Retry-After", out var retryAfterValues))
            return null;

        var retryAfterValue = retryAfterValues.FirstOrDefault();
        if (string.IsNullOrEmpty(retryAfterValue))
            return null;

        // Try to parse as seconds first (most common format)
        if (int.TryParse(retryAfterValue, out var seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        // Try to parse as HTTP date format
        if (DateTimeOffset.TryParse(retryAfterValue, out var retryAfterDate))
        {
            var delay = retryAfterDate - DateTimeOffset.UtcNow;
            return delay.TotalSeconds > 0 ? delay : TimeSpan.Zero;
        }

        _log.Warning("Could not parse Retry-After header value: {0}", retryAfterValue);
        return null;
    }

    public IStash Stash { get; set; } = null!;

    protected override void PostStop()
    {
        _httpClient.Dispose();
    }
}