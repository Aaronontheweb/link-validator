using LinkValidator.Actors;

namespace LinkValidator.Util;

public static class UriHelpers
{
    public static bool CanMakeAbsoluteHttpUri(AbsoluteUri baseUri, string rawUri)
    {
        // this will not return true for things like "mailto:" or "tel:" links
        if (IsAbsoluteUri(rawUri))
            return true;
        try
        {
            var absUri = new Uri(baseUri.Value, rawUri);
            var returnVal = absUri.Scheme.Equals(Uri.UriSchemeHttp) || absUri.Scheme.Equals(Uri.UriSchemeHttps);
            return returnVal;
        }
        catch
        {
            return false;
        }
    }

    public static bool AbsoluteUriIsInDomain(AbsoluteUri baseUrl, AbsoluteUri otherUri)
    {
        return AbsoluteUriIsInDomain(baseUrl, otherUri.Value);
    }

    public static bool AbsoluteUriIsInDomain(AbsoluteUri baseUrl, Uri otherUri)
    {
        return baseUrl.Value.Host == otherUri.Host;
    }

    public static bool IsAbsoluteUri(string rawUri)
    {
        return rawUri.StartsWith(Uri.UriSchemeHttp) || rawUri.StartsWith(Uri.UriSchemeHttps);
    }

    public static AbsoluteUri ToAbsoluteUri(AbsoluteUri baseUri, string rawUri)
    {
        Uri resolvedUri;

        if (!IsAbsoluteUri(rawUri))
        {
            if (rawUri.StartsWith('/')) // have a root-relative Uri
            {
                // get the root of the baseUri
                var rootUri = new Uri(baseUri.Value.GetLeftPart(UriPartial.Authority));
                resolvedUri = new Uri(rootUri, rawUri);
            }
            else // have some other type of relative uri
            {
                var basePath = baseUri.Value.GetLeftPart(UriPartial.Path).TrimEnd('/');

                // Split base path and raw URI into segments
                var baseSegments = basePath.Split('/');
                var relativeSegments = rawUri.Split('/');

                // Result list to accumulate segments
                var finalSegments = new List<string>(baseSegments);

                foreach (var segment in relativeSegments)
                {
                    if (segment == "..")
                    {
                        // Pop last segment if it's not the root
                        if (finalSegments.Count > 3) // Preserve 'http://example.com'
                        {
                            finalSegments.RemoveAt(finalSegments.Count - 1);
                        }
                    }
                    else if (segment != "." && !string.IsNullOrEmpty(segment))
                    {
                        finalSegments.Add(segment);
                    }
                }

                // Rebuild the URI path
                var combinedPath = string.Join("/", finalSegments);
                resolvedUri = new Uri(combinedPath, UriKind.Absolute);
            }
            
        }
        else
        {
            resolvedUri = new Uri(rawUri);
        }

        // Ensure the scheme matches the base URI
        if (resolvedUri.Scheme != baseUri.Value.Scheme)
        {
            var builder = new UriBuilder(resolvedUri)
            {
                Scheme = baseUri.Value.Scheme,
                Port = -1 // Prevents adding the default port
            };
            return new AbsoluteUri(builder.Uri);
        }

        return new AbsoluteUri(resolvedUri);
    }


    public static RelativeUri ToRelativeUri(AbsoluteUri baseUri, AbsoluteUri foundUri)
    {
        var relativeUri = baseUri.Value.MakeRelativeUri(foundUri.Value).ToString();

        // Ensure the relative URI starts with a leading slash
        if (!relativeUri.StartsWith('/'))
        {
            relativeUri = "/" + relativeUri;
        }

        return new RelativeUri(new Uri(relativeUri, UriKind.Relative));
    }
    
    public static bool IsFileUrl(Uri uri)
    {
        // Get the last segment of the path
        var lastSegment = uri.Segments.LastOrDefault();
    
        // Check if the last segment contains a dot, indicating a file extension
        return !string.IsNullOrEmpty(lastSegment) && lastSegment.Contains('.');
    }
    
    public static AbsoluteUri GetDirectoryPath(AbsoluteUri uri)
    {
        // Get the full path part of the URI
        var path = uri.Value.GetLeftPart(UriPartial.Path);

        // If the URL points to a file (contains a dot in the last segment), strip it
        if (IsFileUrl(uri.Value))
        {
            path = path.Substring(0, path.LastIndexOf('/') + 1);
        }

        return new AbsoluteUri(new Uri(path, UriKind.Absolute));
    }

}