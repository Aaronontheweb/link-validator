using System.Collections.Immutable;

namespace LinkValidator.Actors;

public record CrawlUrl(string Url);
public record PageCrawled(string Url, int StatusCode, IReadOnlyCollection<string> Links);
public record CrawlComplete(ImmutableDictionary<string, (int StatusCode, string Path)> Results);

public class CrawlerActor
{
    
}