// -----------------------------------------------------------------------
// <copyright file="CrawlerHelper.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Net;
using Akka.Actor;
using LinkValidator.Actors;

namespace LinkValidator.Util;

/// <summary>
/// Complete report on crawl processing.
/// </summary>
/// <param name="RootUri"></param>
/// <param name="InternalLinks"></param>
/// <param name="ExternalLinks"></param>
public sealed record CrawlReport(
    AbsoluteUri RootUri,
    ImmutableSortedDictionary<string, CrawlRecord> InternalLinks,
    ImmutableSortedDictionary<string, CrawlRecord> ExternalLinks)
{
    /// <summary>
    /// True when the crawl encountered any internal page that is missing or returned a 400+ status
    /// code. This is the condition that <c>--strict</c> uses to fail the run.
    /// </summary>
    public bool HasInternalCrawlErrors => InternalLinks.Any(
        x => (int)x.Value.StatusCode >= (int)HttpStatusCode.BadRequest);
}

public static class CrawlerHelper
{
    public static async Task<CrawlReport> CrawlWebsite(ActorSystem system,
        AbsoluteUri url)
    {
        var crawlSettings = new CrawlConfiguration(url, 10, TimeSpan.FromSeconds(5));
        return await CrawlWebsite(system, url, crawlSettings);
    }
    
    public static async Task<CrawlReport> CrawlWebsite(ActorSystem system,
        AbsoluteUri url, CrawlConfiguration crawlSettings)
    {
        var tcs = new TaskCompletionSource<CrawlReport>();

        var indexer = system.ActorOf(Props.Create(() => new IndexerActor(crawlSettings, tcs)), "indexer");
        indexer.Tell(IndexerActor.BeginIndexing.Instance);
        return await tcs.Task;
    }
}