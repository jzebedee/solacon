#nullable enable

using System;
using System.Globalization;

namespace Solacon;

/// <summary>
/// Represents a decimal value rounded to three fractional digits using midpoint rounding away from zero.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NiceDecimal"/> struct.
/// </remarks>
/// <param name="value">The raw value to round.</param>
public readonly struct NiceDecimal(double value) : ISpanFormattable
{
    private const string DefaultFormat = "0.###";
    private readonly double _value = Math.Round(value, 3, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Gets the rounded numeric value.
    /// </summary>
    public double Value => _value;

    /// <inheritdoc />
    public override string ToString() => _value.ToString(DefaultFormat, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var resolvedFormat = string.IsNullOrEmpty(format) ? DefaultFormat : format;
        return _value.ToString(resolvedFormat, formatProvider ?? CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? formatProvider)
    {
        var resolvedFormat = format.IsEmpty ? DefaultFormat.AsSpan() : format;
        return _value.TryFormat(destination, out charsWritten, resolvedFormat, formatProvider ?? CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts a double to a <see cref="NiceDecimal"/>.
    /// </summary>
    /// <param name="value">The raw value.</param>
    public static implicit operator NiceDecimal(double value) => new(value);

    /// <summary>
    /// Converts a <see cref="NiceDecimal"/> to a double.
    /// </summary>
    /// <param name="value">The rounded value.</param>
    public static implicit operator double(NiceDecimal value) => value._value;
}
