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
        if (Uri.IsWellFormedUriString(rawUri, UriKind.Absolute) && (rawUri.StartsWith(Uri.UriSchemeHttp) || rawUri.StartsWith(Uri.UriSchemeHttps)))
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
    
    public static bool AbsoluteUriIsInDomain(AbsoluteUri baseUrl, Uri otherUri)
    {
        return baseUrl.Value.Host == otherUri.Host;
    }
    
    public static AbsoluteUri ToAbsoluteUri(AbsoluteUri baseUri, string rawUri)
    {
        return new AbsoluteUri(Uri.IsWellFormedUriString(rawUri, UriKind.Absolute)
            ? new Uri(rawUri, UriKind.Absolute)
            : new Uri(baseUri.Value, rawUri));
    }
    
    public static string DenormalizeUrl(Uri baseUrl, string absoluteUrl)
    {
        var uri = new Uri(absoluteUrl);
        var relativePath = Uri.UnescapeDataString(baseUrl.MakeRelativeUri(uri).ToString());
        return string.IsNullOrEmpty(relativePath) ? "/" : relativePath;
    }
}