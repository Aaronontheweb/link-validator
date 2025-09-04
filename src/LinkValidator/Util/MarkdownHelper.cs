// -----------------------------------------------------------------------
// <copyright file="MarkdownHelper.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Net;
using Grynwald.MarkdownGenerator;
using LinkValidator.Actors;

namespace LinkValidator.Util;

public static class MarkdownHelper
{
    public static string GenerateMarkdown(AbsoluteUri baseUri,
        ImmutableSortedDictionary<string, CrawlRecord> results)
    {
        var document = new MdDocument();

        // Add a header
        document.Root.Add(new MdHeading(1, $"Sitemap for [{GetCleanAbsoluteUrl(baseUri)}]"));
        var headerRow = new MdTableRow(new MdTextSpan("URL"), new MdTextSpan("StatusCode"), new MdTextSpan("Linked From"));
        var rows = results
            .Select(kvp => new MdTableRow(
                new MdCodeSpan(kvp.Key), 
                new MdTextSpan(kvp.Value.StatusCode.ToString()),
                new MdTextSpan(FormatLinksToPage(kvp.Value.LinksToPage, baseUri))));

        // Add a table
        document.Root.Add(new MdTable(headerRow, rows));

        // Add broken links summary
        var brokenLinks = results.Where(kvp => kvp.Value.StatusCode != HttpStatusCode.OK).ToList();
        if (brokenLinks.Any())
        {
            document.Root.Add(new MdHeading(2, "🔴 Broken Links Report"));
            
            foreach (var broken in brokenLinks)
            {
                document.Root.Add(new MdHeading(3, $"{broken.Value.StatusCode}: {broken.Key}"));
                
                if (!broken.Value.LinksToPage.IsEmpty)
                {
                    document.Root.Add(new MdParagraph(new MdStrongEmphasisSpan("Fix by updating links in:")));
                    var linkingPages = broken.Value.LinksToPage
                        .Select(link => GetCleanRelativePath(baseUri, link))
                        .Distinct()
                        .OrderBy(x => x)
                        .Select(page => new MdListItem(new MdTextSpan(page)));
                    
                    document.Root.Add(new MdBulletList(linkingPages));
                }
                else
                {
                    document.Root.Add(new MdParagraph(new MdEmphasisSpan("No pages link to this URL (orphaned)")));
                }
            }
        }

        var markdown = document.ToString(new MdSerializationOptions()
        {
            TableStyle = MdTableStyle.GFM
        });
        
        // Post-process to remove unwanted escaping of forward slashes
        return markdown.Replace("\\/", "/");
    }

    private static string FormatLinksToPage(ImmutableList<AbsoluteUri> linksToPage, AbsoluteUri baseUri)
    {
        if (linksToPage.IsEmpty)
            return "-";

        var relativeLinks = linksToPage
            .Select(link => GetCleanRelativePath(baseUri, link))
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        // Show up to 3 linking pages, then "and X more" if there are more
        if (relativeLinks.Count <= 3)
        {
            return string.Join(", ", relativeLinks);
        }

        var firstThree = relativeLinks.Take(3);
        var remaining = relativeLinks.Count - 3;
        return $"{string.Join(", ", firstThree)} +{remaining} more";
    }

    private static string GetCleanAbsoluteUrl(AbsoluteUri uri)
    {
        // Manually construct the URL to avoid Uri escaping
        var scheme = uri.Value.Scheme;
        var host = uri.Value.Host;
        var port = uri.Value.Port;
        var path = uri.Value.AbsolutePath;
        
        var url = $"{scheme}://{host}";
        if ((scheme == "http" && port != 80) || (scheme == "https" && port != 443))
        {
            url += $":{port}";
        }
        url += path;
        
        return url;
    }
    
    private static string GetCleanRelativePath(AbsoluteUri baseUri, AbsoluteUri targetUri)
    {
        // Just use the AbsolutePath to avoid any Uri escaping
        return targetUri.Value.AbsolutePath;
    }
}