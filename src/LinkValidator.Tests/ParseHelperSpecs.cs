using FluentAssertions;
using LinkValidator.Actors;
using LinkValidator.Util;

namespace LinkValidator.Tests;

public class ParseHelperSpecs
{
    // create a string that contains HTML linking to a few different URLs using relative links
    private const string RelativeHtml = """
                                        
                                                <html>
                                                    <head>
                                                        <title>Test Page</title>
                                                    </head>
                                                    <body>
                                                        <a href="/about">About</a>
                                                        <a href="/contact">Contact</a>
                                                        <a href="/faq">FAQ</a>
                                                    </body>
                                                </html>
                                        """;
    
    [Fact]
    public void ParseHelper_should_return_absolute_uris()
    {
        // Arrange
        var uri = new AbsoluteUri(new Uri("http://example.com"));
        
        // Act
        var uris = ParseHelpers.ParseLinks(RelativeHtml, uri);
        
        // Assert
        uris.Should().HaveCount(3);
        uris.Should().Contain(new AbsoluteUri(new Uri("http://example.com/about")));
        uris.Should().Contain(new AbsoluteUri(new Uri("http://example.com/contact")));
        uris.Should().Contain(new AbsoluteUri(new Uri("http://example.com/faq")));
    }
    
    // create a string that contains HTML linking to a few different URLs using absolute links
    private const string AbsoluteHtml = """
                                        
                                                <html>
                                                    <head>
                                                        <title>Test Page</title>
                                                    </head>
                                                    <body>
                                                        <a href="http://example.com/about">About</a>
                                                        <a href="http://example.com/contact">Contact</a>
                                                        <a href="http://example.com/faq">FAQ</a>
                                                    </body>
                                                </html>
                                        """;
    
    [Fact]
    public void ParseHelper_should_return_absolute_uris_when_given_absolute_links()
    {
        // Arrange
        var uri = new AbsoluteUri(new Uri("https://example.com"));
        
        // Act
        var uris = ParseHelpers.ParseLinks(AbsoluteHtml, uri);
        
        // Assert
        uris.Should().HaveCount(3);
        
        // notice that we convert the scheme to https
        uris.Should().Contain(new AbsoluteUri(new Uri("https://example.com/about")));
        uris.Should().Contain(new AbsoluteUri(new Uri("https://example.com/contact")));
        uris.Should().Contain(new AbsoluteUri(new Uri("https://example.com/faq")));
    }
}