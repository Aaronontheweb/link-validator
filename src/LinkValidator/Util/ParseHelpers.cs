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
    public static IReadOnlyList<(AbsoluteUri uri, LinkType type)> ParseLinks(string html, AbsoluteUri baseUrl)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        IReadOnlyList<(AbsoluteUri uri, LinkType type)> links = doc.DocumentNode
            .SelectNodes("//a[@href]")?
            .Where(node => !IsLinkIgnored(node))
            .Select(node => node.GetAttributeValue("href", ""))
            .Where(href => !string.IsNullOrEmpty(href) && CanMakeAbsoluteHttpUri(baseUrl, href))
            .Select(x => ToAbsoluteUri(baseUrl, x))
            .Select(x => (x, AbsoluteUriIsInDomain(baseUrl, x) ? LinkType.Internal : LinkType.External))
            .Distinct() // filter duplicates - we're counting urls, not individual links
            .ToArray() ?? [];
        return links;
    }
    
    private static bool IsLinkIgnored(HtmlNode linkNode)
    {
        // Check if link is within an ignore block
        if (IsWithinIgnoreBlock(linkNode))
            return true;
        
        // Check if previous sibling is an ignore-next comment
        var previousNode = linkNode.PreviousSibling;
        while (previousNode != null)
        {
            if (previousNode.NodeType == HtmlNodeType.Comment)
            {
                var commentNode = (HtmlCommentNode)previousNode;
                var commentText = commentNode.Comment.Trim();
                // Remove comment delimiters and trim
                commentText = commentText.Replace("<!--", "").Replace("-->", "").Trim();
                // Check for standalone ignore (not "begin")
                if (commentText.Equals("link-validator-ignore", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (previousNode.NodeType == HtmlNodeType.Element)
            {
                // Stop looking if we hit another element
                break;
            }
            previousNode = previousNode.PreviousSibling;
        }
        
        return false;
    }
    
    private static bool IsWithinIgnoreBlock(HtmlNode node)
    {
        // Start from the node and walk up the tree
        var current = node;
        
        while (current != null)
        {
            // Check if there's an ignore block at this level
            if (current.ParentNode != null)
            {
                var siblings = current.ParentNode.ChildNodes;
                var inIgnoreBlock = false;
                
                foreach (var sibling in siblings)
                {
                    // Check for comment nodes
                    if (sibling.NodeType == HtmlNodeType.Comment)
                    {
                        var commentNode = (HtmlCommentNode)sibling;
                        var commentText = commentNode.Comment.Trim();
                        // Remove comment delimiters and trim
                        commentText = commentText.Replace("<!--", "").Replace("-->", "").Trim();
                        
                        if (commentText.Equals("begin link-validator-ignore", StringComparison.OrdinalIgnoreCase))
                        {
                            inIgnoreBlock = true;
                        }
                        else if (commentText.Equals("end link-validator-ignore", StringComparison.OrdinalIgnoreCase))
                        {
                            inIgnoreBlock = false;
                        }
                    }
                    
                    // If we've reached the current node and we're in an ignore block, return true
                    if ((sibling == current || sibling.Descendants().Contains(node)) && inIgnoreBlock)
                    {
                        return true;
                    }
                }
            }
            
            current = current.ParentNode;
        }
        
        return false;
    }
}