// -----------------------------------------------------------------------
// <copyright file="RelativeUriSpecs.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using LinkValidator.Actors;

namespace LinkValidator.Tests;

public class RelativeUriSpecs
{
    public RelativeUri Uri1 { get; } = new(new Uri("/path", UriKind.Relative));

    [Fact]
    public void RelativeUri_should_throw_when_not_relative()
    {
        // Arrange
        var uri = new Uri("http://example.com", UriKind.RelativeOrAbsolute);

        // Act
        Action act = () => new RelativeUri(uri);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RelativeUri_should_equal_copy_of_itself()
    {
        // Arrange
        var uri2 = new RelativeUri(new Uri(Uri1.Value.ToString(), UriKind.Relative));

        // Assert
        uri2.Should().Be(Uri1);
        Uri1.GetHashCode().Should().Be(uri2.GetHashCode());
    }

    [Fact]
    public void RelativeUri_should_print_path()
    {
        // Arrange
        var uri = new RelativeUri(new Uri("/path-to-file.html", UriKind.Relative));

        // Act
        var result = uri.ToString();

        // Assert
        result.Should().Be("/path-to-file.html");
    }
}