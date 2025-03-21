// -----------------------------------------------------------------------
// <copyright file="CrawlConfiguration.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

namespace LinkValidator.Actors;

/// <summary>
/// Configuration for the crawler
/// </summary>
public sealed record CrawlConfiguration(AbsoluteUri BaseUrl, int MaxInflightRequests, TimeSpan RequestTimeout)
{
    /// <summary>
    /// The absolute base url - we are only interested in urls stemming from it.
    /// </summary>
    public AbsoluteUri BaseUrl { get; } = BaseUrl;

    /// <summary>
    /// Max degree of parallelism.
    /// </summary>
    public int MaxInflightRequests { get; } = MaxInflightRequests;

    /// <summary>
    /// The amount of time we'll allot for any individual HTTP request
    /// </summary>
    public TimeSpan RequestTimeout { get; } = RequestTimeout;
}