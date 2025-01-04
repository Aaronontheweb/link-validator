// -----------------------------------------------------------------------
// <copyright file="ParseHelpers.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

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
            .Distinct() // filter duplicates - we're counting urls, not individual links
            .ToArray() ?? [];
        return links;
    }
}