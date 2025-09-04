// -----------------------------------------------------------------------
// <copyright file="CrawlerHelper.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
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
    ImmutableSortedDictionary<string, CrawlRecord> ExternalLinks);

public static class CrawlerHelper
{
    public static async Task<CrawlReport> CrawlWebsite(ActorSystem system,
        AbsoluteUri url)
    {
        var crawlSettings = new CrawlConfiguration(url, 10, TimeSpan.FromSeconds(5));
        var tcs = new TaskCompletionSource<CrawlReport>();

        var indexer = system.ActorOf(Props.Create(() => new IndexerActor(crawlSettings, tcs)), "indexer");
        indexer.Tell(IndexerActor.BeginIndexing.Instance);
        return await tcs.Task;
    }
}