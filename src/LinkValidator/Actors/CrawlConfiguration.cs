namespace LinkValidator.Actors;

public record struct AbsoluteUri
{
    public AbsoluteUri(Uri value)
    {
        Value = value;
        if (!value.IsAbsoluteUri)
            throw new ArgumentException("Value must be an absolute URL", nameof(value));
    }

    public Uri Value { get; }
}

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