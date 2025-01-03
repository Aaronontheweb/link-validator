using System.Collections.Immutable;
using System.CommandLine;
using System.Net;
using Akka.Actor;
using LinkValidator.Actors;
using static LinkValidator.Util.DiffHelper;
using static LinkValidator.Util.MarkdownHelper;

namespace LinkValidator;

class Program
{
    public static async Task<int> Main(string[] args)
    {
        var urlOption = new Option<string>("--url", "The URL to crawl") { IsRequired = true };
        var outputOption = new Option<string?>("--output", "Optional output file path for the sitemap");
        var diffOption = new Option<string?>("--diff", "Previous sitemap file to compare against");
        var strictOption = new Option<bool>("--strict", () => false,
            "Return error code if pages are missing or returning 400+ status codes");

        var rootCommand = new RootCommand("Website crawler and sitemap generator")
        {
            urlOption,
            outputOption,
            diffOption,
            strictOption
        };
        
        rootCommand.SetHandler(async (url, output, diff, strict) =>
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                await Console.Error.WriteLineAsync($"Invalid URL [{url}] - must be an absolute uri.");
                Environment.Exit(1);
                return;
            }
           
            var system = ActorSystem.Create("CrawlerSystem", "akka.loglevel = INFO");
            var absoluteUri = new AbsoluteUri(new Uri(url));
            var results = await CrawlWebsite(system, absoluteUri);
            var markdown = GenerateMarkdown(absoluteUri, results);

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
        }, urlOption, outputOption, diffOption, strictOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task<ImmutableSortedDictionary<string, HttpStatusCode>> CrawlWebsite(ActorSystem system, AbsoluteUri url)
    {
        var crawlSettings = new CrawlConfiguration(url, 10, TimeSpan.FromSeconds(5));
        var tcs = new TaskCompletionSource<ImmutableSortedDictionary<string, HttpStatusCode>>();
        
        var indexer = system.ActorOf(Props.Create(() => new IndexerActor(crawlSettings, tcs)), "indexer");
        indexer.Tell(IndexerActor.BeginIndexing.Instance);
        return await tcs.Task;
    }
}