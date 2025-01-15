// -----------------------------------------------------------------------
// <copyright file="CrawlRecord.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using System.Net;

namespace LinkValidator.Actors;

public record struct CrawlRecord(AbsoluteUri PageCrawled, HttpStatusCode StatusCode, IReadOnlyList<AbsoluteUri> LinksToPage);