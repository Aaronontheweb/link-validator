// -----------------------------------------------------------------------
// <copyright file="Program.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using System.CommandLine;
using Akka.Actor;
using LinkValidator.Actors;
using LinkValidator.Util;
using static LinkValidator.Util.DiffHelper;
using static LinkValidator.Util.MarkdownHelper;

namespace LinkValidator;

class Program
{
    private static int GetMaxExternalRetries()
    {
        var envValue = Environment.GetEnvironmentVariable("LINK_VALIDATOR_MAX_EXTERNAL_RETRIES");
        return int.TryParse(envValue, out var value) ? value : 3;
    }

    private static int GetRetryDelaySeconds()
    {
        var envValue = Environment.GetEnvironmentVariable("LINK_VALIDATOR_RETRY_DELAY_SECONDS");
        return int.TryParse(envValue, out var value) ? value : 10;
    }

    public static async Task<int> Main(string[] args)
    {
        var urlOption = new Option<string>("--url", "The URL to crawl") { IsRequired = true };
        var outputOption = new Option<string?>("--output", "Optional output file path for the sitemap");
        var diffOption = new Option<string?>("--diff", "Previous output file to compare against");
        var strictOption = new Option<bool>("--strict", () => false,
            "Return error code if pages are missing or returning 400+ status codes");
        var maxRetriesOption = new Option<int>("--max-external-retries", GetMaxExternalRetries,
            "Maximum retry attempts for external URLs returning 429 (default: 3)");
        var retryDelayOption = new Option<int>("--retry-delay-seconds", GetRetryDelaySeconds,
            "Default retry delay in seconds when no Retry-After header is present (default: 10)");

        var rootCommand = new RootCommand(
            "LinkValidator: used to crawl a website and report on both internal / link status." + Environment.NewLine + " Use in CI/CD pipelines to find broken links.")
        {
            urlOption,
            outputOption,
            diffOption,
            strictOption,
            maxRetriesOption,
            retryDelayOption
        };

        rootCommand.SetHandler(async (url, output, diff, strict, maxRetries, retryDelay) =>
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                await Console.Error.WriteLineAsync($"Invalid URL [{url}] - must be an absolute uri.");
                Environment.Exit(1);
                return;
            }

            var system = ActorSystem.Create("CrawlerSystem", "akka.loglevel = INFO");
            var absoluteUri = new AbsoluteUri(new Uri(url));
            var crawlSettings = new CrawlConfiguration(
                absoluteUri, 
                10, 
                TimeSpan.FromSeconds(5), 
                maxRetries, 
                TimeSpan.FromSeconds(retryDelay));
            var results = await CrawlerHelper.CrawlWebsite(system, absoluteUri, crawlSettings);
            var markdown = GenerateMarkdown(results);

            _ = system.Terminate();

            if (output != null)
            {
                await File.WriteAllTextAsync(output, markdown);
            }
            else
            {
                Console.WriteLine(markdown);
            }

            if (!string.IsNullOrEmpty(diff))
            {
                var previousMarkdown = await File.ReadAllTextAsync(diff);
                var (differences, hasErrors) = CompareSitemapsWithErrors(previousMarkdown, markdown);
                foreach (var difference in differences)
                {
                    Console.WriteLine(difference);
                }

                if (strict && hasErrors)
                {
                    Environment.Exit(1);
                }
            }
        }, urlOption, outputOption, diffOption, strictOption, maxRetriesOption, retryDelayOption);

        return await rootCommand.InvokeAsync(args);
    }
}