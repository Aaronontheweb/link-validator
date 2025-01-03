using HtmlAgilityPack;
using LinkValidator.Actors;

namespace LinkValidator.Util;

public static class ParseHelpers
{
    public static IReadOnlyList<string> ParseLinks(string html, AbsoluteUri baseUrl)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        IReadOnlyList<string> links = doc.DocumentNode
            .SelectNodes("//a[@href]")?
            .Select(node => node.GetAttributeValue("href", ""))
            .Where(href => !string.IsNullOrEmpty(href))
            // .Where(href => href.StartsWith(baseUrl))
            // .Select(href => UriHelpers.NormalizeUrl(baseUrl, href))
            .ToArray() ?? [];
        return links;
    }
}