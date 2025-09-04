// -----------------------------------------------------------------------
// <copyright file="CrawlerHelper.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using Akka.Actor;
using LinkValidator.Actors;

namespace LinkValidator.Util;

public static class CrawlerHelper
{
    public static async Task<ImmutableSortedDictionary<string, CrawlRecord>> CrawlWebsite(ActorSystem system,
        AbsoluteUri url)
    {
        var crawlSettings = new CrawlConfiguration(url, 10, TimeSpan.FromSeconds(5));
        var tcs = new TaskCompletionSource<ImmutableSortedDictionary<string, CrawlRecord>>();

        var indexer = system.ActorOf(Props.Create(() => new IndexerActor(crawlSettings, tcs)), "indexer");
        indexer.Tell(IndexerActor.BeginIndexing.Instance);
        return await tcs.Task;
    }
}