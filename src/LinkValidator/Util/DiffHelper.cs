// -----------------------------------------------------------------------
// <copyright file="DiffHelper.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace LinkValidator.Util;

public static partial class DiffHelper
{
    public static (IReadOnlyList<string> Differences, bool HasErrors) CompareSitemapsWithErrors(string previous,
        string current)
    {
        var differences = new List<string>();
        var hasErrors = false;

        var previousLines = previous.Split('\n')
            .Skip(2)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        var currentLines = current.Split('\n')
            .Skip(2)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        // Check for missing pages
        foreach (var line in previousLines.Except(currentLines))
        {
            differences.Add($"Missing: {line}");
            hasErrors = true;
        }

        // Check for new pages
        foreach (var line in currentLines.Except(previousLines))
        {
            differences.Add($"New: {line}");

            // Check if new page has error status code
            var statusCodeMatch = MyRegex().Match(line);
            if (statusCodeMatch.Success && int.Parse(statusCodeMatch.Groups[1].Value) >= 400)
            {
                hasErrors = true;
            }
        }

        return (differences, hasErrors);
    }

    [GeneratedRegex(@"\|\s*(\d{3})\s*\|")]
    private static partial Regex MyRegex();
}