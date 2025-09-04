// -----------------------------------------------------------------------
// <copyright file="TestWebServerFixture.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.Net;

namespace LinkValidator.Tests;

public class TestWebServerFixture : IAsyncDisposable
{
    private IWebHost? _webHost;
    private readonly object _lock = new();
    private const int TestPort = 8080;

    public string? BaseUrl { get; private set; }

    public TestWebServerFixture StartServer(string contentDirectory)
    {
        lock (_lock)
        {
            if (_webHost != null)
                throw new InvalidOperationException("Server is already started");

            if (!Directory.Exists(contentDirectory))
                throw new DirectoryNotFoundException($"Content directory not found: {contentDirectory}");

            var fullPath = Path.GetFullPath(contentDirectory);

            _webHost = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, TestPort);
                })
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                })
                .Configure(app =>
                {
                    app.UseDefaultFiles(new DefaultFilesOptions
                    {
                        FileProvider = new PhysicalFileProvider(Path.GetFullPath(contentDirectory))
                    });
                    app.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new PhysicalFileProvider(Path.GetFullPath(contentDirectory)),
                        RequestPath = ""
                    });

                    app.UseRouting();
                })
                .Build();

            _webHost.Start();
            BaseUrl = $"http://localhost:{TestPort}";

            return this;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_webHost != null)
        {
            await _webHost.StopAsync();
            _webHost.Dispose();
            _webHost = null;
        }
        BaseUrl = null;
    }
}