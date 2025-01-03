using LinkValidator.Actors;

namespace LinkValidator.Util;

public static class UriHelpers
{
    public static string NormalizeUrl(string baseUrl, string href)
    {
        if (Uri.TryCreate(new Uri(baseUrl), href, out Uri? absoluteUri))
        {
            return absoluteUri.GetLeftPart(UriPartial.Path);
        }

        return href;
    }

    public static bool CanMakeAbsoluteHttpUri(AbsoluteUri baseUri, string rawUri)
    {
        // this will not return true for things like "mailto:" or "tel:" links
        if (Uri.IsWellFormedUriString(rawUri, UriKind.Absolute) &&
            (rawUri.StartsWith(Uri.UriSchemeHttp) || rawUri.StartsWith(Uri.UriSchemeHttps)))
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

    public static AbsoluteUri ToAbsoluteUri(AbsoluteUri baseUri, string rawUri)
    {
        if (!Uri.IsWellFormedUriString(rawUri, UriKind.Absolute))
        {
            return new AbsoluteUri(new Uri(baseUri.Value, rawUri));
        }

        var resultUri = new Uri(rawUri);

        // Ensure the scheme matches the base URI
        if (resultUri.Scheme != baseUri.Value.Scheme)
        {
            var builder = new UriBuilder(resultUri)
            {
                Scheme = baseUri.Value.Scheme,
                Port = -1  // Prevents adding the default port
            };
            return new AbsoluteUri(builder.Uri);
        }

        return new AbsoluteUri(resultUri);
    }

    public static RelativeUri ToRelativeUri(AbsoluteUri baseUri, AbsoluteUri foundUri)
    {
        var relativeUri = baseUri.Value.MakeRelativeUri(foundUri.Value).ToString();

        // Ensure the relative URI starts with a leading slash
        if (!relativeUri.StartsWith("/"))
        {
            relativeUri = "/" + relativeUri;
        }

        return new RelativeUri(new Uri(relativeUri, UriKind.Relative));
    }
}