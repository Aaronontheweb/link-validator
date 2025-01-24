// -----------------------------------------------------------------------
// <copyright file="CrawlRecord.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Net;

namespace LinkValidator.Actors;

public record struct CrawlRecord(
    AbsoluteUri PageCrawled,
    HttpStatusCode StatusCode,
    ImmutableList<AbsoluteUri> LinksToPage)
{
    public static CrawlRecord Empty(AbsoluteUri pageCrawled) => new(pageCrawled, HttpStatusCode.ServiceUnavailable, ImmutableList<AbsoluteUri>.Empty);
}