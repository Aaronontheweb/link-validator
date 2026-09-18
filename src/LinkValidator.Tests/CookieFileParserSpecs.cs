// -----------------------------------------------------------------------
// <copyright file="CookieFileParserSpecs.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
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

        Assert.Single(cookies.Cast<Cookie>());
        Assert.Equal("session", cookies[0].Name);
        Assert.Equal("abc123", cookies[0].Value);
        Assert.Equal("example.com", cookies[0].Domain);
        Assert.Equal("/", cookies[0].Path);
        Assert.False(cookies[0].Secure);
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

        Assert.Single(cookies.Cast<Cookie>());
        Assert.Equal(".AspNetCore.Cookies", cookies[0].Name);
        Assert.Equal("CfDJ8test", cookies[0].Value);
        Assert.Equal("localhost", cookies[0].Domain);
    }

    [Fact]
    public void ShouldParseSecureCookie()
    {
        WriteFile("""
            example.com	FALSE	/secure	TRUE	1900000000	token	xyz789
            """);

        var container = CookieFileParser.Parse(_tempFile);
        var cookies = container.GetAllCookies();

        Assert.Single(cookies.Cast<Cookie>());
        Assert.True(cookies[0].Secure);
        Assert.Equal("/secure", cookies[0].Path);
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
        Assert.Single(container.GetAllCookies().Cast<Cookie>());
    }

    [Fact]
    public void ShouldSkipEmptyLines()
    {
        WriteFile("""
            example.com	FALSE	/	FALSE	1900000000	cookie1	value1

            example.com	FALSE	/api	FALSE	1900000000	cookie2	value2

            """);

        var container = CookieFileParser.Parse(_tempFile);
        Assert.Equal(2, container.GetAllCookies().Count);
    }

    [Fact]
    public void ShouldSkipMalformedLines()
    {
        WriteFile("""
            this-line-has-too-few-columns
            example.com	FALSE	/	FALSE	1900000000	valid	cookie
            """);

        var container = CookieFileParser.Parse(_tempFile);
        Assert.Single(container.GetAllCookies().Cast<Cookie>());
    }

    [Fact]
    public void ShouldReturnEmptyContainerForEmptyFile()
    {
        WriteFile(string.Empty);

        var container = CookieFileParser.Parse(_tempFile);
        Assert.Empty(container.GetAllCookies().Cast<Cookie>());
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

        Assert.Equal(2, cookies.Count);
        var names = cookies.Cast<Cookie>().Select(c => c.Name).ToList();
        Assert.Contains(".AspNetCore.Cookies", names);
        Assert.Contains("XSRF-TOKEN", names);
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

        Assert.Single(cookies.Cast<Cookie>());
        Assert.True(cookies[0].Expires > DateTime.Now);
    }
}
