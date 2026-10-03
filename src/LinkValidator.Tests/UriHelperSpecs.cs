// -----------------------------------------------------------------------
// <copyright file="UriHelperSpecs.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

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
        Assert.Equal(expected, result);
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
        Assert.True(result);
    }

    // write an inverse of the previous test
    [Theory]
    [InlineData("http://example.com", "http://example.org/some/path")]
    public void AbsoluteUriIsInDomain_should_NOT_return_true_when_host_is_different(string baseUri, string testUri)
    {
        // Arrange
        var baseUrl = new AbsoluteUri(new Uri(baseUri));
        var otherUri = new Uri(testUri);

        // Act
        var result = UriHelpers.AbsoluteUriIsInDomain(baseUrl, otherUri);

        // Assert
        Assert.False(result);
    }

    public static readonly TheoryData<AbsoluteUri, string, AbsoluteUri> ToAbsoluteUriData = new()
    {
        {
            new AbsoluteUri(new Uri("http://example.com")), "http://example.com/some/path",
            new AbsoluteUri(new Uri("http://example.com/some/path"))
        },
        {
            new AbsoluteUri(new Uri("http://example.com")), "/some/path",
            new AbsoluteUri(new Uri("http://example.com/some/path"))
        },
        {
            new AbsoluteUri(new Uri("http://example.com")), "some/path",
            new AbsoluteUri(new Uri("http://example.com/some/path"))
        },
        {
            new AbsoluteUri(new Uri("https://example.com")), "some/path",
            new AbsoluteUri(new Uri("https://example.com/some/path"))
        },

        // relative uris with a folder
        {
            new AbsoluteUri(new Uri("http://example.com/some")), "../path",
            new AbsoluteUri(new Uri("http://example.com/path"))
        },
        {
            new AbsoluteUri(new Uri("http://example.com/some/fart")), "../path",
            new AbsoluteUri(new Uri("http://example.com/some/path"))
        },
        {
            new AbsoluteUri(new Uri("http://example.com/some/llm/rag")), "../../path",
            new AbsoluteUri(new Uri("http://example.com/some/path"))
        },

        // relative uris with a file
        {
            new AbsoluteUri(new Uri("http://example.com/some.html")), "../path.html",
            new AbsoluteUri(new Uri("http://example.com/path.html"))
        },
        {
            new AbsoluteUri(new Uri("http://example.com/some/fart.html")), "../path",
            new AbsoluteUri(new Uri("http://example.com/some/path"))
        },
        {
            new AbsoluteUri(new Uri("http://example.com/some/llm/rag.html")), "../../path",
            new AbsoluteUri(new Uri("http://example.com/some/path"))
        },

        // root-relative uris
        // relative uris with a folder
        {
            new AbsoluteUri(new Uri("http://example.com/some")), "/path",
            new AbsoluteUri(new Uri("http://example.com/path"))
        },
        {
            new AbsoluteUri(new Uri("http://example.com/some/fart")), "/path/bar",
            new AbsoluteUri(new Uri("http://example.com/path/bar"))
        },

        // HTTP vs. HTTPS - should normalize to use whatever the baseUri uses
        {
            new AbsoluteUri(new Uri("https://example.com")), "http://example.com/some/path",
            new AbsoluteUri(new Uri("https://example.com/some/path"))
        },
        {
            new AbsoluteUri(new Uri("http://example.com")), "https://example.com/some/path",
            new AbsoluteUri(new Uri("http://example.com/some/path"))
        }
    };

    [Theory]
    [MemberData(nameof(ToAbsoluteUriData))]
    public void ToAbsoluteUri_should_return_expected_results(AbsoluteUri baseUri, string rawUri, AbsoluteUri expected)
    {
        // Act
        var result = UriHelpers.ToAbsoluteUri(baseUri, rawUri);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("https://example.com/path?foo=bar", "https://example.com/path?foo=bar")]
    [InlineData("https://www.youtube.com/playlist?list=PLsYDwCTwskWduFC6QwS8lm6xlCnvejmHV", "https://www.youtube.com/playlist?list=PLsYDwCTwskWduFC6QwS8lm6xlCnvejmHV")]
    // internal URLs are normalized to the base URI's scheme (https)
    [InlineData("http://example.com/some/path?query=1#fragment", "https://example.com/some/path?query=1")]
    [InlineData("http://example.com/some/path#fragment", "https://example.com/some/path")]
    public void ToAbsoluteUri_should_preserve_query_and_strip_fragment(string rawUri, string expected)
    {
        // Arrange
        var baseUri = new AbsoluteUri(new Uri("https://example.com"));

        // Act
        var result = UriHelpers.ToAbsoluteUri(baseUri, rawUri);

        // Assert
        Assert.Equal(new AbsoluteUri(new Uri(expected)), result);
    }

    [Theory]
    [InlineData("http://example.com/some/path", "http://example.com/some/path")]
    [InlineData("http://example.com/some/path?query=1", "http://example.com/some/path?query=1")]
    [InlineData("http://example.com/some/path#fragment", "http://example.com/some/path")]
    [InlineData("http://example.com/some/path?query=1#fragment", "http://example.com/some/path?query=1")]
    // F2: query-string percent-escapes must survive fragment stripping unchanged.
    // (Path escapes are already canonicalized by System.Uri at construction, so we
    // only guarantee the query portion is preserved.)
    [InlineData("http://example.com/path?q=a%20b#frag", "http://example.com/path?q=a%20b")]
    // F3: an empty-query 'path?#frag' must not retain a trailing '?'.
    [InlineData("http://example.com/path?#frag", "http://example.com/path")]
    public void RemoveFragment_should_preserve_query_and_strip_fragment(string input, string expected)
    {
        // Arrange
        var uri = new Uri(input);

        // Act
        var result = UriHelpers.RemoveFragment(uri);

        // Assert
        // Compare AbsoluteUri (the wire/request form) so percent-escapes in the
        // query are compared as-is rather than via ToString()'s decoded display.
        Assert.Equal(expected, result.AbsoluteUri);
    }

    public static readonly TheoryData<AbsoluteUri, string, RelativeUri> ToRelativeUriData = new()
    {
        {
            new AbsoluteUri(new Uri("http://example.com")), "http://example.com/some/path",
            new RelativeUri(new Uri("/some/path", UriKind.Relative))
        },
        {
            new AbsoluteUri(new Uri("http://example.com")), "https://example.com/some/path",
            new RelativeUri(new Uri("/some/path", UriKind.Relative))
        },
        {
            new AbsoluteUri(new Uri("http://example.com")), "/some/path",
            new RelativeUri(new Uri("/some/path", UriKind.Relative))
        },
        {
            new AbsoluteUri(new Uri("http://example.com")), "some/path",
            new RelativeUri(new Uri("/some/path", UriKind.Relative))
        },
        {
            new AbsoluteUri(new Uri("https://example.com")), "some/path",
            new RelativeUri(new Uri("/some/path", UriKind.Relative))
        },

        // HTTP vs. HTTPS - should normalize to use whatever the baseUri uses
        {
            new AbsoluteUri(new Uri("https://example.com")), "http://example.com/some/path",
            new RelativeUri(new Uri("/some/path", UriKind.Relative))
        },
        {
            new AbsoluteUri(new Uri("http://example.com")), "https://example.com/some/path",
            new RelativeUri(new Uri("/some/path", UriKind.Relative))
        }
    };

    [Theory]
    [MemberData(nameof(ToRelativeUriData))]
    public void ToRelativeUri_should_return_expected_results(AbsoluteUri baseUri, string rawUri, RelativeUri expected)
    {
        // Act
        var absoluteUri = UriHelpers.ToAbsoluteUri(baseUri, rawUri);
        var result = UriHelpers.ToRelativeUri(baseUri, absoluteUri);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("http://example.com/some/path.html", true)]
    [InlineData("http://example.com/some/path", false)]
    [InlineData("http://example.com/some/path/", false)]
    [InlineData("http://example.com/some/path.html?query=1", true)]
    [InlineData("http://example.com/some/path.html#fragment", true)]
    [InlineData("http://example.com/some/path.html?query=1#fragment", true)]
    public void IsFileUrl_should_return_expected_results(string uri, bool expected)
    {
        // Arrange
        var testUri = new Uri(uri);

        // Act
        var result = UriHelpers.IsFileUrl(testUri);

        // Assert
        Assert.Equal(expected, result);
    }

    public static readonly TheoryData<AbsoluteUri, AbsoluteUri> GetDirectoryData = new()
    {
        {
            new AbsoluteUri(new Uri("https://example.com/foo/bar")),
            new AbsoluteUri(new Uri("https://example.com/foo/bar"))
        },
        {
            new AbsoluteUri(new Uri("https://example.com/foo/bar/index.html")),
            new AbsoluteUri(new Uri("https://example.com/foo/bar/"))
        }
    };

    [Theory]
    [MemberData(nameof(GetDirectoryData))]
    public void GetDirectory_should_return_expected_results(AbsoluteUri actualUri, AbsoluteUri expectedUri)
    {
        // Act
        var testUri = UriHelpers.GetDirectoryPath(actualUri);

        // Assert
        Assert.Equal(expectedUri, testUri);
    }
}