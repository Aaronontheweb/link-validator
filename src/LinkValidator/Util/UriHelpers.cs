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
}