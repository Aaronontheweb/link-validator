// -----------------------------------------------------------------------
// <copyright file="AbsoluteUriSpecs.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using LinkValidator.Actors;

namespace LinkValidator.Tests;

/// <summary>
/// These are mostly sanity check specs to ensure that the AbsoluteUri struct behaves as expected.
/// </summary>
public class AbsoluteUriSpecs
{
    public AbsoluteUri Uri1 { get; } = new(new Uri("http://example.com"));

    [Fact]
    public void AbsoluteUri_should_throw_when_not_absolute()
    {
        // Arrange
        var uri = new Uri("example.com", UriKind.RelativeOrAbsolute);

        // Act
        Action act = () => new AbsoluteUri(uri);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AbsoluteUri_should_equal_copy_of_itself()
    {
        // Arrange
        var uri2 = new AbsoluteUri(new Uri(Uri1.Value.ToString()));

        // Assert
        uri2.Should().Be(Uri1);
        Uri1.GetHashCode().Should().Be(uri2.GetHashCode());
    }

    [Fact]
    public void Absolute_should_print_path()
    {
        // Arrange
        var uri = new AbsoluteUri(new Uri("https://example.com/path-to-file.html", UriKind.Absolute));

        // Act
        var result = uri.ToString();

        // Assert
        result.Should().Be("https://example.com/path-to-file.html");
    }
}