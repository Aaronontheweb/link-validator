using System.Text;

namespace LinkValidator.Actors;

public record struct AbsoluteUri
{
    public AbsoluteUri(Uri value)
    {
        Value = value;
        if (!value.IsAbsoluteUri)
            throw new ArgumentException("Value must be an absolute URL", nameof(value));
    }

    public Uri Value { get; }
}

/// <summary>
/// This is what we use in our final outputs for uniqueness and comparison
/// </summary>
public record struct RelativeUri
{
    public RelativeUri(Uri value)
    {
        Value = value;
        if (value.IsAbsoluteUri)
            throw new ArgumentException("Value must be a relative URL", nameof(value));
    }

    public Uri Value { get; }
    
    public override string ToString() => Value.ToString();
}