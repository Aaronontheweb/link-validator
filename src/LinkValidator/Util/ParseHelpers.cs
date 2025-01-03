using HtmlAgilityPack;
using LinkValidator.Actors;
using static LinkValidator.Util.UriHelpers;

namespace LinkValidator.Util;

public static class ParseHelpers
{
    public static IReadOnlyList<AbsoluteUri> ParseLinks(string html, AbsoluteUri baseUrl)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        IReadOnlyList<AbsoluteUri> links = doc.DocumentNode
            .SelectNodes("//a[@href]")?
            .Select(node => node.GetAttributeValue("href", ""))
            .Where(href => !string.IsNullOrEmpty(href) && CanMakeAbsoluteHttpUri(baseUrl, href))
            .Select(x => ToAbsoluteUri(baseUrl, x))
            .Where(x => AbsoluteUriIsInDomain(baseUrl, x))
            .ToArray() ?? [];
        return links;
    }
}