using static System.String;

namespace Mtpr.Core;

public abstract class Enumeration(string? value, string displayName)
    : IComparable, 
        IComparable<Enumeration>,
        IEquatable<Enumeration>
{
    #region Properties

    public string? Value { get; } = value;
    public string DisplayName { get; } = displayName;

    #endregion

    #region Constructors

    protected Enumeration(string displayName) : this(null, displayName)
    {
    }

    #endregion

    #region Public Methods

    public int CompareTo(object? other)
    {
        if (other is null)
            return 1;

        if (other is not Enumeration enumeration)
            throw new ArgumentException(
                $"Object must be of type {nameof(Enumeration)}.", nameof(other));

        return CompareTo(enumeration);
    }

    public int CompareTo(Enumeration? other)
    {
        if (other is null)
            return 1;

        if (GetType() != other.GetType())
            throw new ArgumentException(
                $"Cannot compare '{GetType().Name}' with '{other.GetType().Name}'.", nameof(other));

        var result = Compare(Value, other.Value, StringComparison.Ordinal);

        if (result != 0)
            return result;

        return Value is null
            ? Compare(DisplayName, other.DisplayName, StringComparison.Ordinal)
            : 0;
    }

    public bool Equals(Enumeration? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return GetType() == other.GetType() && CompareTo(other) == 0;
    }

    public override bool Equals(object? obj) => Equals(obj as Enumeration);

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Value, Value is null ? DisplayName : null);
    }

    public override string ToString()
    {
        return DisplayName;
    }

    #endregion
}
