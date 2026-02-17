// -----------------------------------------------------------------------
// <copyright file="CrawlConfiguration.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using System.Net;

namespace LinkValidator.Actors;

/// <summary>
/// Configuration for the crawler
/// </summary>
public sealed record CrawlConfiguration(AbsoluteUri BaseUrl, int MaxInflightRequests, TimeSpan RequestTimeout, int MaxExternalRetries = 3, TimeSpan DefaultExternalRetryDelay = default, CookieContainer? Cookies = null)
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

    /// <summary>
    /// Maximum number of retries for external requests that return 429 TooManyRequests
    /// </summary>
    public int MaxExternalRetries { get; } = MaxExternalRetries;

    /// <summary>
    /// Default delay for retrying external requests when no Retry-After header is present
    /// </summary>
    public TimeSpan DefaultExternalRetryDelay { get; } = DefaultExternalRetryDelay == default ? TimeSpan.FromSeconds(10) : DefaultExternalRetryDelay;

    /// <summary>
    /// Optional cookie container for authenticated crawling (e.g. from a Netscape cookie file).
    /// </summary>
    public CookieContainer? Cookies { get; } = Cookies;
}