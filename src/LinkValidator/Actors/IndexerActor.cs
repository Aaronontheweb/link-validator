﻿// -----------------------------------------------------------------------
// <copyright file="IndexerActor.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Net;
using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using LinkValidator.Util;

namespace LinkValidator.Actors;

public enum CrawlStatus
{
    /// <summary>
    /// No one has been requested to crawl this document yet
    /// </summary>
    NotVisited = 0,

    /// <summary>
    /// Someone is currently crawling this document
    /// </summary>
    Visiting = 1,

    /// <summary>
    /// We failed to crawl this document for some reason
    /// </summary>
    Failed = 2,

    /// <summary>
    /// We've crawled this document
    /// </summary>
    Visited = 3
}

public sealed class IndexerActor : UntypedActor, IWithTimers
{
    public sealed class BeginIndexing
    {
        private BeginIndexing()
        {
        }

        public static BeginIndexing Instance { get; } = new();
    }

    public sealed class CheckCompletion
    {
        private CheckCompletion()
        {
        }

        public static CheckCompletion Instance { get; } = new();
    }

    public sealed class ReportStatistics
    {
        private ReportStatistics()
        {
        }

        public static ReportStatistics Instance { get; } = new();
    }

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly CrawlConfiguration _crawlConfiguration;
    private IActorRef _crawlers = ActorRefs.Nobody;
    private readonly TaskCompletionSource<ImmutableSortedDictionary<string, HttpStatusCode>> _completionSource;

    public IndexerActor(CrawlConfiguration crawlConfiguration,
        TaskCompletionSource<ImmutableSortedDictionary<string, HttpStatusCode>> completionSource)
    {
        _crawlConfiguration = crawlConfiguration;
        _completionSource = completionSource;
    }

    public Dictionary<AbsoluteUri, (CrawlStatus status, HttpStatusCode?)> IndexedDocuments { get; } = new();

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case BeginIndexing:
                _log.Info("Beginning indexing of [{0}]", _crawlConfiguration.BaseUrl);
                IndexedDocuments[_crawlConfiguration.BaseUrl] = (CrawlStatus.Visiting, null);
                _crawlers.Tell(new CrawlUrl(_crawlConfiguration.BaseUrl));
                break;
            case PageCrawled pageCrawled:
            {
                IndexedDocuments[pageCrawled.Url] = (CrawlStatus.Visited, pageCrawled.StatusCode);

                // kick off scans of all the links on this page
                foreach (var p in pageCrawled.Links)
                    if (!IndexedDocuments.TryGetValue(p, out var status) || status.status == CrawlStatus.NotVisited)
                    {
                        IndexedDocuments[p] = (CrawlStatus.Visiting, null);
                        _crawlers.Tell(new CrawlUrl(p));
                    }

                if (IsCrawlComplete)
                {
                    var pagesByStatusCode =
                        IndexedDocuments.Values.CountBy(c => c.Item2 ?? HttpStatusCode.ServiceUnavailable)
                            .Select(c => $"{c.Key}:{c.Value}");
                    ;
                    _log.Info("Crawl complete: {0}", string.Join(", ", pagesByStatusCode));

                    var finalOutput = IndexedDocuments
                        .Where(x => x.Value.status == CrawlStatus.Visited)
                        .ToImmutableSortedDictionary(
                            x => UriHelpers.ToRelativeUri(_crawlConfiguration.BaseUrl, x.Key).ToString(),
                            x => x.Value.Item2 ?? HttpStatusCode.NotFound);

                    _completionSource.SetResult(finalOutput);

                    Context.Stop(Self);
                }

                break;
            }
        }
    }

    private bool IsCrawlComplete => IndexedDocuments.Values.All(x => x.status == CrawlStatus.Visited);

    protected override void PreStart()
    {
        _crawlers = Context.ActorOf(Props.Create<CrawlerActor>(_crawlConfiguration, Self)
            .WithRouter(new RoundRobinPool(5)));
        Timers.StartPeriodicTimer("CheckMetrics", ReportStatistics.Instance, TimeSpan.FromSeconds(5));
    }

    public ITimerScheduler Timers { get; set; } = null!;
}