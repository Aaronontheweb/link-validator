// -----------------------------------------------------------------------
// <copyright file="CookieFileParserSpecs.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using LinkValidator.Util;

namespace LinkValidator.Tests;

public class CookieFileParserSpecs : IDisposable
{
    private readonly string _tempFile;

    public CookieFileParserSpecs()
    {
        _tempFile = Path.GetTempFileName();
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }

    private void WriteFile(string content) => File.WriteAllText(_tempFile, content);

    [Fact]
    public void ShouldParseStandardNetscapeCookie()
    {
        WriteFile("""
            # Netscape HTTP Cookie File
            example.com	FALSE	/	FALSE	1900000000	session	abc123
            """);

        var container = CookieFileParser.Parse(_tempFile);
        var cookies = container.GetAllCookies();

        cookies.Should().HaveCount(1);
        cookies[0].Name.Should().Be("session");
        cookies[0].Value.Should().Be("abc123");
        cookies[0].Domain.Should().Be("example.com");
        cookies[0].Path.Should().Be("/");
        cookies[0].Secure.Should().BeFalse();
    }

    [Fact]
    public void ShouldParseHttpOnlyPrefixedCookie()
    {
        // curl writes HttpOnly cookies with #HttpOnly_ prefix
        WriteFile("""
            # Netscape HTTP Cookie File
            #HttpOnly_localhost	FALSE	/	FALSE	1900000000	.AspNetCore.Cookies	CfDJ8test
            """);

        var container = CookieFileParser.Parse(_tempFile);
        var cookies = container.GetAllCookies();

        cookies.Should().HaveCount(1);
        cookies[0].Name.Should().Be(".AspNetCore.Cookies");
        cookies[0].Value.Should().Be("CfDJ8test");
        cookies[0].Domain.Should().Be("localhost");
    }

    [Fact]
    public void ShouldParseSecureCookie()
    {
        WriteFile("""
            example.com	FALSE	/secure	TRUE	1900000000	token	xyz789
            """);

        var container = CookieFileParser.Parse(_tempFile);
        var cookies = container.GetAllCookies();

        cookies.Should().HaveCount(1);
        cookies[0].Secure.Should().BeTrue();
        cookies[0].Path.Should().Be("/secure");
    }

    [Fact]
    public void ShouldSkipCommentLines()
    {
        WriteFile("""
            # This is a comment
            # Another comment
            example.com	FALSE	/	FALSE	1900000000	valid	cookie
            """);

        var container = CookieFileParser.Parse(_tempFile);
        container.GetAllCookies().Should().HaveCount(1);
    }

    [Fact]
    public void ShouldSkipEmptyLines()
    {
        WriteFile("""
            example.com	FALSE	/	FALSE	1900000000	cookie1	value1

            example.com	FALSE	/api	FALSE	1900000000	cookie2	value2

            """);

        var container = CookieFileParser.Parse(_tempFile);
        container.GetAllCookies().Should().HaveCount(2);
    }

    [Fact]
    public void ShouldSkipMalformedLines()
    {
        WriteFile("""
            this-line-has-too-few-columns
            example.com	FALSE	/	FALSE	1900000000	valid	cookie
            """);

        var container = CookieFileParser.Parse(_tempFile);
        container.GetAllCookies().Should().HaveCount(1);
    }

    [Fact]
    public void ShouldReturnEmptyContainerForEmptyFile()
    {
        WriteFile(string.Empty);

        var container = CookieFileParser.Parse(_tempFile);
        container.GetAllCookies().Should().BeEmpty();
    }

    [Fact]
    public void ShouldParseMultipleCookies()
    {
        WriteFile("""
            # Netscape HTTP Cookie File
            #HttpOnly_localhost	FALSE	/	FALSE	1900000000	.AspNetCore.Cookies	token123
            localhost	FALSE	/	FALSE	1900000000	XSRF-TOKEN	csrf456
            """);

        var container = CookieFileParser.Parse(_tempFile);
        var cookies = container.GetAllCookies();

        cookies.Should().HaveCount(2);
        cookies.Select(c => c.Name).Should().Contain(".AspNetCore.Cookies").And.Contain("XSRF-TOKEN");
    }

    [Fact]
    public void ShouldParseZeroExpiryAsOneDay()
    {
        // A zero expiry should produce a cookie that expires in ~1 day (session-like)
        WriteFile("""
            example.com	FALSE	/	FALSE	0	session	abc
            """);

        var container = CookieFileParser.Parse(_tempFile);
        var cookies = container.GetAllCookies();

        cookies.Should().HaveCount(1);
        cookies[0].Expires.Should().BeAfter(DateTime.Now);
    }
}
