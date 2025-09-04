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
    public static string GenerateMarkdown(CrawlReport results)
    {
        var document = new MdDocument();

        // Add a header
        document.Root.Add(new MdHeading(1, new MdTextSpan("Sitemap for "),
            new MdCodeSpan(GetCleanAbsoluteUrl(results.RootUri))));

        // Internal Pages
        document.Root.Add(new MdHeading(2, new MdTextSpan("Internal Pages")));
        var header1Row = new MdTableRow(new MdTextSpan("URL"), new MdTextSpan("StatusCode"),
            new MdTextSpan("Linked From"));
        var internalRows = results.InternalLinks
            .Select(kvp => new MdTableRow(
                new MdCodeSpan(kvp.Key),
                new MdTextSpan(kvp.Value.StatusCode.ToString()),
                FormatLinksToPageAsMarkdown(kvp.Value.LinksToPage, results.RootUri)));

        document.Root.Add(new MdTable(header1Row, internalRows));

        // Add broken links summary organized by source page
        var brokenLinks = results.InternalLinks.Where(kvp => kvp.Value.StatusCode >= HttpStatusCode.BadRequest)
            .Concat(results.ExternalLinks.Where(c => c.Value.StatusCode >= HttpStatusCode.BadRequest)).ToList();
        if (brokenLinks.Count != 0)
        {
            document.Root.Add(new MdHeading(2, "🔴 Pages with Broken Links"));

            // Group broken links by the pages that link to them
            var pagesBrokenLinks = brokenLinks
                .SelectMany(broken => broken.Value.LinksToPage
                    .Select(linkingPage => new
                    {
                        SourcePage = GetCleanRelativePath(results.RootUri, linkingPage),
                        BrokenUrl = broken.Key,
                        StatusCode = broken.Value.StatusCode
                    }))
                .GroupBy(x => x.SourcePage)
                .OrderBy(g => g.Key)
                .ToList();

            if (pagesBrokenLinks.Any())
            {
                foreach (var pageGroup in pagesBrokenLinks)
                {
                    document.Root.Add(new MdHeading(3, new MdCodeSpan(pageGroup.Key),
                        new MdTextSpan(" has broken links:")));

                    var brokenLinksForPage = pageGroup
                        .OrderBy(x => x.BrokenUrl)
                        .Select(x => new MdListItem(
                            new MdCodeSpan(x.BrokenUrl),
                            new MdTextSpan($" ({x.StatusCode})")))
                        .ToList();

                    document.Root.Add(new MdBulletList(brokenLinksForPage));
                }
            }

            // Handle orphaned broken links (no pages link to them)
            var orphanedLinks = brokenLinks
                .Where(broken => broken.Value.LinksToPage.IsEmpty)
                .ToList();

            if (orphanedLinks.Any())
            {
                document.Root.Add(new MdHeading(3, "🔗 Orphaned broken URLs"));
                document.Root.Add(
                    new MdParagraph(new MdEmphasisSpan("These URLs are broken but no pages link to them:")));

                var orphanedItems = orphanedLinks
                    .OrderBy(x => x.Key)
                    .Select(x => new MdListItem(
                        new MdCodeSpan(x.Key),
                        new MdTextSpan($" ({x.Value.StatusCode})")))
                    .ToList();

                document.Root.Add(new MdBulletList(orphanedItems));
            }
        }

        var markdown = document.ToString(new MdSerializationOptions()
        {
            TableStyle = MdTableStyle.GFM
        });

        // Post-process to remove unwanted escaping of forward slashes
        return markdown.Replace("\\/", "/");
    }

    private static MdSpan FormatLinksToPageAsMarkdown(ImmutableList<AbsoluteUri> linksToPage, AbsoluteUri baseUri)
    {
        if (linksToPage.IsEmpty)
            return new MdTextSpan("-");

        var relativeLinks = linksToPage
            .Select(link => GetCleanRelativePath(baseUri, link))
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        // Create a composite span with code spans for URLs and text spans for separators
        var spans = new List<MdSpan>();

        var linksToShow = relativeLinks.Take(3).ToList();
        for (int i = 0; i < linksToShow.Count; i++)
        {
            spans.Add(new MdCodeSpan(linksToShow[i]));
            if (i < linksToShow.Count - 1)
            {
                spans.Add(new MdTextSpan(", "));
            }
        }

        if (relativeLinks.Count > 3)
        {
            var remaining = relativeLinks.Count - 3;
            spans.Add(new MdTextSpan($" +{remaining} more"));
        }

        return new MdCompositeSpan(spans);
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