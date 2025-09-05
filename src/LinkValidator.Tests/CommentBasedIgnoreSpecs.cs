// -----------------------------------------------------------------------
// <copyright file="CommentBasedIgnoreSpecs.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using LinkValidator.Actors;
using LinkValidator.Util;
using Xunit;

namespace LinkValidator.Tests;

public class CommentBasedIgnoreSpecs
{
    private readonly AbsoluteUri _baseUrl = new(new Uri("https://example.com/"));

    [Fact]
    public void Should_ignore_next_link_with_comment()
    {
        var html = @"
            <html>
                <body>
                    <a href=""https://www.google.com"">Normal Link</a>
                    <!-- link-validator-ignore-next -->
                    <a href=""http://localhost:3000"">Ignored Link</a>
                    <a href=""https://www.github.com"">Another Normal Link</a>
                </body>
            </html>";

        var links = ParseHelpers.ParseLinks(html, _baseUrl);

        links.Should().HaveCount(2);
        links.Should().Contain(x => x.uri.Value.Host == "www.google.com");
        links.Should().Contain(x => x.uri.Value.Host == "www.github.com");
        links.Should().NotContain(x => x.uri.Value.Host == "localhost");
    }

    [Fact]
    public void Should_ignore_all_links_within_comment_block()
    {
        var html = @"
            <html>
                <body>
                    <a href=""https://www.google.com"">Normal Link</a>
                    <!-- link-validator-ignore -->
                    <div>
                        <a href=""http://localhost:3000"">Ignored Link 1</a>
                        <p>Some text with <a href=""http://localhost:9090"">Ignored Link 2</a></p>
                        <ul>
                            <li><a href=""http://localhost:16686"">Ignored Link 3</a></li>
                        </ul>
                    </div>
                    <!-- /link-validator-ignore -->
                    <a href=""https://www.github.com"">Another Normal Link</a>
                </body>
            </html>";

        var links = ParseHelpers.ParseLinks(html, _baseUrl);

        links.Should().HaveCount(2);
        links.Should().Contain(x => x.uri.Value.Host == "www.google.com");
        links.Should().Contain(x => x.uri.Value.Host == "www.github.com");
        links.Should().NotContain(x => x.uri.Value.Host == "localhost");
    }

    [Fact]
    public void Should_handle_nested_ignore_blocks()
    {
        var html = @"
            <html>
                <body>
                    <!-- link-validator-ignore -->
                    <a href=""http://localhost:3000"">Ignored Link 1</a>
                    <div>
                        <a href=""http://localhost:9090"">Ignored Link 2</a>
                    </div>
                    <!-- /link-validator-ignore -->
                    <div>
                        <a href=""https://www.google.com"">Normal Link</a>
                    </div>
                </body>
            </html>";

        var links = ParseHelpers.ParseLinks(html, _baseUrl);

        links.Should().HaveCount(1);
        links.Should().Contain(x => x.uri.Value.Host == "www.google.com");
        links.Should().NotContain(x => x.uri.Value.Host == "localhost");
    }

    [Fact]
    public void Should_only_ignore_next_immediate_link()
    {
        var html = @"
            <html>
                <body>
                    <!-- link-validator-ignore-next -->
                    <a href=""http://localhost:3000"">Ignored Link</a>
                    <a href=""http://localhost:9090"">Not Ignored</a>
                    <a href=""https://www.google.com"">Normal Link</a>
                </body>
            </html>";

        var links = ParseHelpers.ParseLinks(html, _baseUrl);

        links.Should().HaveCount(2);
        links.Should().Contain(x => x.uri.Value.ToString().Contains("localhost:9090"));
        links.Should().Contain(x => x.uri.Value.Host == "www.google.com");
        links.Should().NotContain(x => x.uri.Value.ToString().Contains("localhost:3000"));
    }

    [Fact]
    public void Should_handle_multiple_ignore_blocks()
    {
        var html = @"
            <html>
                <body>
                    <a href=""https://www.google.com"">Normal Link 1</a>
                    <!-- link-validator-ignore -->
                    <a href=""http://localhost:3000"">Ignored Link 1</a>
                    <!-- /link-validator-ignore -->
                    <a href=""https://www.github.com"">Normal Link 2</a>
                    <!-- link-validator-ignore -->
                    <a href=""http://localhost:9090"">Ignored Link 2</a>
                    <!-- /link-validator-ignore -->
                    <a href=""https://www.stackoverflow.com"">Normal Link 3</a>
                </body>
            </html>";

        var links = ParseHelpers.ParseLinks(html, _baseUrl);

        links.Should().HaveCount(3);
        links.Should().Contain(x => x.uri.Value.Host == "www.google.com");
        links.Should().Contain(x => x.uri.Value.Host == "www.github.com");
        links.Should().Contain(x => x.uri.Value.Host == "www.stackoverflow.com");
        links.Should().NotContain(x => x.uri.Value.Host == "localhost");
    }

    [Fact]
    public void Should_be_case_insensitive_for_comments()
    {
        var html = @"
            <html>
                <body>
                    <!-- LINK-VALIDATOR-IGNORE-NEXT -->
                    <a href=""http://localhost:3000"">Ignored Link 1</a>
                    <!-- Link-Validator-Ignore -->
                    <a href=""http://localhost:9090"">Ignored Link 2</a>
                    <!-- /LINK-VALIDATOR-IGNORE -->
                    <a href=""https://www.google.com"">Normal Link</a>
                </body>
            </html>";

        var links = ParseHelpers.ParseLinks(html, _baseUrl);

        links.Should().HaveCount(1);
        links.Should().Contain(x => x.uri.Value.Host == "www.google.com");
        links.Should().NotContain(x => x.uri.Value.Host == "localhost");
    }
}