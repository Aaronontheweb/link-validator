// -----------------------------------------------------------------------
// <copyright file="CookieFileParser.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using System.Net;

namespace LinkValidator.Util;

/// <summary>
/// Parses Netscape/Mozilla format cookie files (e.g. produced by <c>curl -c cookies.txt &lt;url&gt;</c>).
/// </summary>
public static class CookieFileParser
{
    /// <summary>
    /// Parses a Netscape tab-delimited cookie file and returns a populated <see cref="CookieContainer"/>.
    /// </summary>
    /// <remarks>
    /// Format per line: domain TAB flag TAB path TAB secure TAB expiry TAB name TAB value
    /// Lines starting with '#' or empty lines are ignored.
    /// </remarks>
    public static CookieContainer Parse(string filePath)
    {
        var container = new CookieContainer();
        foreach (var rawLine in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            // curl writes HttpOnly cookies with a "#HttpOnly_" prefix — strip it before parsing.
            var line = rawLine.StartsWith("#HttpOnly_", StringComparison.OrdinalIgnoreCase)
                ? rawLine["#HttpOnly_".Length..]
                : rawLine;

            // Skip genuine comment lines (but not the #HttpOnly_ ones already handled above).
            if (line.StartsWith('#'))
                continue;

            var parts = line.Split('\t');
            if (parts.Length < 7)
                continue;

            var domain = parts[0].TrimStart('.');
            var path = parts[2];
            var secure = parts[3].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            var name = parts[5];
            var value = parts[6];

            long.TryParse(parts[4], out var expiryUnix);
            var expires = expiryUnix > 0
                ? DateTimeOffset.FromUnixTimeSeconds(expiryUnix).UtcDateTime
                : DateTime.Now.AddDays(1);

            var cookie = new Cookie(name, value, path, domain)
            {
                Secure = secure,
                Expires = expires
            };
            container.Add(cookie);
        }
        return container;
    }
}
