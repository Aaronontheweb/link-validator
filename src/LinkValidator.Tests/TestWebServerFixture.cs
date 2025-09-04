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
    public Action<string>? Logger { get; set; }

    public TestWebServerFixture StartServer(string contentDirectory, int port = TestPort)
    {
        lock (_lock)
        {
            if (_webHost != null)
                return this; // Allow multiple calls, return existing server

            if (!Directory.Exists(contentDirectory))
                throw new DirectoryNotFoundException($"Content directory not found: {contentDirectory}");

            var fullPath = Path.GetFullPath(contentDirectory);

            _webHost = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, port);
                    options.Limits.MaxConcurrentConnections = 100;
                    options.Limits.MaxConcurrentUpgradedConnections = 100;
                })
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                })
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        Logger?.Invoke($"Request: {context.Request.Method} {context.Request.Path}");
                        await next();
                        Logger?.Invoke($"Response: {context.Response.StatusCode} for {context.Request.Path}");
                    });
                    
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
            BaseUrl = $"http://localhost:{port}";

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