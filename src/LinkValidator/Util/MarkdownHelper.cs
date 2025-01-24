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
        document.Root.Add(new MdHeading(1, $"Sitemap for [{baseUri.Value.ToString()}]"));
        var headerRow = new MdTableRow(new MdTextSpan("URL"), new MdTextSpan("StatusCode"));
        var rows = results
            .Select(kvp => new MdTableRow(new MdCodeSpan(kvp.Key), 
                new MdTextSpan(kvp.Value.StatusCode.ToString())));

        // Add a table
        document.Root.Add(new MdTable(headerRow, rows));

        return document.ToString(new MdSerializationOptions()
        {
            TableStyle = MdTableStyle.GFM
        });
    }
}