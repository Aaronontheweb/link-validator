using FluentAssertions;
using LinkValidator.Actors;
using LinkValidator.Util;

namespace LinkValidator.Tests;

public class UriHelperSpecs
{
    [Theory]
    [InlineData("http://example.com", "http://example.com/some/path")]
    [InlineData("https://example.com", "http://example.com/some/path")]
    [InlineData("https://example.com", "https://example.com/some/path")]
    [InlineData("http://example.com", "https://example.com/some/path")]
    public void AbsoluteUriIsInDomain_should_return_true_when_host_is_same(string baseUri, string testUri)
    {
        // Arrange
        var baseUrl = new AbsoluteUri(new Uri(baseUri)); 
        var otherUri = new Uri(testUri);
        
        // Act
        var result = UriHelpers.AbsoluteUriIsInDomain(baseUrl, otherUri);
        
        // Assert
        result.Should().BeTrue();
    }
    
    // write an inverse of the previous test
    [Theory]
    [InlineData("http://example.com", "http://example.org/some/path")]
    public void AbsoluteUriIsInDomain_should_NOT_return_true_when_host_is_diffrent(string baseUri, string testUri)
    {
        // Arrange
        var baseUrl = new AbsoluteUri(new Uri(baseUri)); 
        var otherUri = new Uri(testUri);
        
        // Act
        var result = UriHelpers.AbsoluteUriIsInDomain(baseUrl, otherUri);
        
        // Assert
        result.Should().BeFalse();
    }

    
}