using FluentAssertions;
using LinkValidator.Actors;
using LinkValidator.Util;

namespace LinkValidator.Tests;

public class UriHelperSpecs
{
    public static readonly TheoryData<AbsoluteUri, string, bool> CanMakeAbsoluteUriData = new()
    {
        { new AbsoluteUri(new Uri("http://example.com")), "http://example.com/some/path", true },
        { new AbsoluteUri(new Uri("http://example.com")), "https://example.com/some/path", true },
        { new AbsoluteUri(new Uri("http://example.com")), "/some/path", true },
        { new AbsoluteUri(new Uri("http://example.com")), "mailto:fart@example.com", false }
    };
    
    [Theory]
    [MemberData(nameof(CanMakeAbsoluteUriData))]
    public void CanMakeAbsoluteUri_should_return_expected_results(AbsoluteUri baseUri, string rawUri, bool expected)
    {
        // Act
        var result = UriHelpers.CanMakeAbsoluteHttpUri(baseUri, rawUri);
        
        // Assert
        result.Should().Be(expected);
    }
    
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
    
    public static readonly TheoryData<AbsoluteUri, string, AbsoluteUri> ToAbsoluteUriData = new()
    {
        { new AbsoluteUri(new Uri("http://example.com")), "http://example.com/some/path", new AbsoluteUri(new Uri("http://example.com/some/path")) },
        { new AbsoluteUri(new Uri("http://example.com")), "/some/path", new AbsoluteUri(new Uri("http://example.com/some/path")) },
        { new AbsoluteUri(new Uri("http://example.com")), "some/path", new AbsoluteUri(new Uri("http://example.com/some/path")) },
        { new AbsoluteUri(new Uri("https://example.com")), "some/path", new AbsoluteUri(new Uri("https://example.com/some/path")) },
        
        // HTTP vs. HTTPS - should normalize to use whatever the baseUri uses
        { new AbsoluteUri(new Uri("https://example.com")), "http://example.com/some/path", new AbsoluteUri(new Uri("https://example.com/some/path")) },
        { new AbsoluteUri(new Uri("http://example.com")), "https://example.com/some/path", new AbsoluteUri(new Uri("http://example.com/some/path")) }
    };
    
    [Theory]
    [MemberData(nameof(ToAbsoluteUriData))]
    public void ToAbsoluteUri_should_return_expected_results(AbsoluteUri baseUri, string rawUri, AbsoluteUri expected)
    {
        // Act
        var result = UriHelpers.ToAbsoluteUri(baseUri, rawUri);
        
        // Assert
        result.Should().Be(expected);
    }
}