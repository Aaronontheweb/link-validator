// -----------------------------------------------------------------------
// <copyright file="CrawlStatus.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

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