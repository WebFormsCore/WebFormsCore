using System;
using System.Globalization;

namespace WebFormsCore.UI.WebControls;

/// <summary>Represents a length measurement.</summary>
[Serializable]
public struct Unit
{
    /// <summary>Represents an empty <see cref="T:WebFormsCore.UI.WebControls.Unit" />. This field is read-only.</summary>
    public static readonly Unit Empty;

    internal const int MaxValue = 32767;
    internal const int MinValue = -32768;
    private readonly UnitType _type;
    private readonly double _value;

    /// <summary>Initializes a new instance of the <see cref="T:WebFormsCore.UI.WebControls.Unit" /> structure with the specified 32-bit signed integer.</summary>
    /// <param name="value">A 32-bit signed integer that represents the length of the <see cref="T:WebFormsCore.UI.WebControls.Unit" /> in pixels. </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="value" /> is not between -32768 and 32767. </exception>
    public Unit(int value)
    {
        _value = value >= short.MinValue && value <= short.MaxValue
            ? (double)value
            : throw new ArgumentOutOfRangeException(nameof(value));
        _type = UnitType.Pixel;
    }

    /// <summary>Initializes a new instance of the <see cref="T:WebFormsCore.UI.WebControls.Unit" /> structure with the specified double precision floating point number.</summary>
    /// <param name="value">A double precision floating point number that represents the length of the <see cref="T:WebFormsCore.UI.WebControls.Unit" /> in pixels. </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="value" /> is not between -32768 and 32767. </exception>
    public Unit(double value)
    {
        this._value = value >= short.MinValue && value <= short.MaxValue
            ? (double)(int)value
            : throw new ArgumentOutOfRangeException(nameof(value));
        _type = UnitType.Pixel;
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.WebControls.Unit" /> structure with the specified double precision floating point number and <see cref="T:System.Web.UI.WebControls.UnitType" />.</summary>
    /// <param name="value">A double precision floating point number that represents the length of the <see cref="T:System.Web.UI.WebControls.Unit" />. </param>
    /// <param name="type">One of the <see cref="T:System.Web.UI.WebControls.UnitType" /> enumeration values. </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="value" /> is not between -32768 and 32767. </exception>
    public Unit(double value, UnitType type)
    {
        if (value < short.MinValue || value > short.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value));
        this._value = type != UnitType.Pixel ? value : (int)value;
        this._type = type;
    }

    /// <summary>Initializes a new instance of the <see cref="T:WebFormsCore.UI.WebControls.Unit" /> structure with the specified length.</summary>
    /// <param name="value">A string that represents the length of the <see cref="T:WebFormsCore.UI.WebControls.Unit" />. </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">The specified length is not between -32768 and 32767. </exception>
    /// <exception cref="T:System.FormatException">
    /// <paramref name="value" /> is not a valid CSS-compliant unit expression. </exception>
    public Unit(ReadOnlySpan<char> value)
        : this(value, CultureInfo.CurrentCulture, UnitType.Pixel)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="T:WebFormsCore.UI.WebControls.Unit" /> structure with the specified length and <see cref="T:System.Globalization.CultureInfo" />.</summary>
    /// <param name="value">A string that represents the length of the <see cref="T:WebFormsCore.UI.WebControls.Unit" />. </param>
    /// <param name="culture">A <see cref="T:System.Globalization.CultureInfo" /> that represents the culture. </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">The specified length is not between -32768 and 32767. </exception>
    /// <exception cref="T:System.FormatException">
    /// <paramref name="value" /> is not a valid CSS-compliant unit expression. </exception>
    public Unit(ReadOnlySpan<char> value, CultureInfo? culture)
        : this(value, culture, UnitType.Pixel)
    {
    }

    internal Unit(ReadOnlySpan<char> span, CultureInfo? culture, UnitType defaultType)
    {
        if (span.Length == 0)
        {
            _value = 0.0;
            _type = 0;
        }
        else
        {
            culture ??= CultureInfo.CurrentCulture;

            var length = span.Length;
            var num = -1;
            for (var index = 0; index < length; ++index)
            {
                var ch = span[index];

                if (ch is not (>= '0' and <= '9' or '-' or '.' or ','))
                {
                    break;
                }

                num = index;
            }

            if (num == -1)
            {
                throw new FormatException("Invalid Unit");
            }

            _type = num >= length - 1 ? defaultType : GetTypeFromString(span.Slice(num + 1).Trim());
            var text = span.Slice(0, num + 1);
            try
            {
                _value = float.Parse(text, NumberStyles.Float, culture);

                if (_type == UnitType.Pixel)
                    _value = (int)_value;
            }
            catch
            {
                throw new FormatException("Invalid Unit");
            }

            if (_value is < short.MinValue or > short.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(_value));
            }
        }
    }

    /// <summary>Gets a value indicating whether the <see cref="T:WebFormsCore.UI.WebControls.Unit" /> is empty.</summary>
    /// <returns>
    /// <see langword="true" /> if the <see cref="T:WebFormsCore.UI.WebControls.Unit" /> is empty; otherwise, <see langword="false" />.</returns>
    public bool IsEmpty => _type == 0;

    /// <summary>Gets the unit type of the <see cref="T:System.Web.UI.WebControls.Unit" />.</summary>
    /// <returns>One of the <see cref="T:System.Web.UI.WebControls.UnitType" /> enumeration values. The default is <see cref="F:System.Web.UI.WebControls.UnitType.Pixel" />.</returns>
    public UnitType Type => !IsEmpty ? _type : UnitType.Pixel;

    /// <summary>Gets the length of the <see cref="T:WebFormsCore.UI.WebControls.Unit" />.</summary>
    /// <returns>A double-precision floating point number that represents the length of the <see cref="T:WebFormsCore.UI.WebControls.Unit" />.</returns>
    public double Value => _value;

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode() => IsEmpty ? 0 : _value.GetHashCode() ^ _type.GetHashCode();

    /// <summary>Compares this <see cref="T:WebFormsCore.UI.WebControls.Unit" /> with the specified object.</summary>
    /// <param name="obj">The object for comparison. </param>
    /// <returns>
    /// <see langword="true" /> if the <see cref="T:WebFormsCore.UI.WebControls.Unit" /> that this method is called from is equal to the specified object; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj) =>
        obj is Unit unit && unit._type == _type && Math.Abs(unit._value - _value) < 0.0001;

    /// <summary>Compares two <see cref="T:WebFormsCore.UI.WebControls.Unit" /> objects to determine whether they are equal.</summary>
    /// <param name="left">The <see cref="T:WebFormsCore.UI.WebControls.Unit" /> on the left side of the operator. </param>
    /// <param name="right">The <see cref="T:WebFormsCore.UI.WebControls.Unit" /> on the right side of the operator. </param>
    /// <returns>
    /// <see langword="true" /> if both <see cref="T:WebFormsCore.UI.WebControls.Unit" /> objects are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(Unit left, Unit right) =>
        left._type == right._type && Math.Abs(left._value - right._value) < 0.0001;

    /// <summary>Compares two <see cref="T:WebFormsCore.UI.WebControls.Unit" /> objects to determine whether they are not equal.</summary>
    /// <param name="left">The <see cref="T:WebFormsCore.UI.WebControls.Unit" /> on the left side of the operator. </param>
    /// <param name="right">The <see cref="T:WebFormsCore.UI.WebControls.Unit" /> on the right side of the operator. </param>
    /// <returns>
    /// <see langword="true" /> if the <see cref="T:WebFormsCore.UI.WebControls.Unit" /> objects are not equal; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(Unit left, Unit right) =>
        left._type != right._type || Math.Abs(left._value - right._value) > 0.0001;

    private static string GetStringFromType(UnitType type)
    {
        switch (type)
        {
            case UnitType.Pixel:
                return "px";
            case UnitType.Point:
                return "pt";
            case UnitType.Pica:
                return "pc";
            case UnitType.Inch:
                return "in";
            case UnitType.Mm:
                return "mm";
            case UnitType.Cm:
                return "cm";
            case UnitType.Percentage:
                return "%";
            case UnitType.Em:
                return "em";
            case UnitType.Ex:
                return "ex";
            default:
                return string.Empty;
        }
    }

    private static UnitType GetTypeFromString(ReadOnlySpan<char> value)
    {
        if (value.Length == 0) return UnitType.Pixel;
        if (value.Length == 1 && value[0] == '%') return UnitType.Percentage;
        if (value.Length != 2) throw new ArgumentOutOfRangeException(nameof(value));

        if (value[0] is 'p' or 'P' && value[1] is 'x' or 'X')
            return UnitType.Pixel;
        if (value[0] is 'p' or 'P' && value[1] is 't' or 'T')
            return UnitType.Percentage;
        if (value[0] is 'p' or 'P' && value[1] is 'c' or 'C')
            return UnitType.Pica;
        if (value[0] is 'i' or 'I' && value[1] is 'n' or 'N')
            return UnitType.Inch;
        if (value[0] is 'm' or 'M' && value[1] is 'm' or 'M')
            return UnitType.Mm;
        if (value[0] is 'c' or 'C' && value[1] is 'm' or 'M')
            return UnitType.Cm;
        if (value[0] is 'e' or 'E' && value[1] is 'm' or 'M')
            return UnitType.Em;
        if (value[0] is 'e' or 'E' && value[1] is 'x' or 'X')
            return UnitType.Ex;
        throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <summary>Converts the specified string to a <see cref="T:WebFormsCore.UI.WebControls.Unit" />.</summary>
    /// <param name="s">The string to convert. </param>
    /// <returns>A <see cref="T:WebFormsCore.UI.WebControls.Unit" /> that represents the specified string.</returns>
    public static Unit Parse(string s) => new(s.AsSpan(), CultureInfo.CurrentCulture);

    /// <summary>Converts the specified string and <see cref="T:System.Globalization.CultureInfo" /> to a <see cref="T:WebFormsCore.UI.WebControls.Unit" />.</summary>
    /// <param name="s">The string to convert. </param>
    /// <param name="culture">A <see cref="T:System.Globalization.CultureInfo" /> object that represents the culture. </param>
    /// <returns>A <see cref="T:WebFormsCore.UI.WebControls.Unit" /> that represents the specified string.</returns>
    public static Unit Parse(string s, CultureInfo culture) => new(s.AsSpan(), culture);

    /// <summary>Converts the specified string to a <see cref="T:WebFormsCore.UI.WebControls.Unit" />.</summary>
    /// <param name="s">The string to convert. </param>
    /// <returns>A <see cref="T:WebFormsCore.UI.WebControls.Unit" /> that represents the specified string.</returns>
    public static Unit Parse(ReadOnlySpan<char> s) => new(s, CultureInfo.CurrentCulture);

    /// <summary>Converts the specified string and <see cref="T:System.Globalization.CultureInfo" /> to a <see cref="T:WebFormsCore.UI.WebControls.Unit" />.</summary>
    /// <param name="s">The string to convert. </param>
    /// <param name="culture">A <see cref="T:System.Globalization.CultureInfo" /> object that represents the culture. </param>
    /// <returns>A <see cref="T:WebFormsCore.UI.WebControls.Unit" /> that represents the specified string.</returns>
    public static Unit Parse(ReadOnlySpan<char> s, CultureInfo culture) => new(s, culture);

    /// <summary>Creates a <see cref="T:System.Web.UI.WebControls.Unit" /> of type <see cref="F:System.Web.UI.WebControls.UnitType.Percentage" /> from the specified double-precision floating-point number.</summary>
    /// <param name="n">A double-precision floating-point number that represents the length of the <see cref="T:System.Web.UI.WebControls.Unit" />.</param>
    /// <returns>A <see cref="T:System.Web.UI.WebControls.Unit" /> of type <see cref="F:System.Web.UI.WebControls.UnitType.Percentage" /> that represents the length specified by the double-precision floating-point number.</returns>
    public static Unit Percentage(double n) => new(n, UnitType.Percentage);

    /// <summary>Creates a <see cref="T:System.Web.UI.WebControls.Unit" /> of type <see cref="F:System.Web.UI.WebControls.UnitType.Pixel" /> from the specified 32-bit signed integer.</summary>
    /// <param name="n">A 32-bit signed integer that represents the length of the <see cref="T:System.Web.UI.WebControls.Unit" />. </param>
    /// <returns>A <see cref="T:System.Web.UI.WebControls.Unit" /> of type <see cref="F:System.Web.UI.WebControls.UnitType.Pixel" /> that represents the length specified by the <paramref name="n" /> parameter.</returns>
    public static Unit Pixel(int n) => new(n);

    /// <summary>Creates a <see cref="T:System.Web.UI.WebControls.Unit" /> of type <see cref="F:System.Web.UI.WebControls.UnitType.Point" /> from the specified 32-bit signed integer.</summary>
    /// <param name="n">A 32-bit signed integer that represents the length of the <see cref="T:System.Web.UI.WebControls.Unit" />. </param>
    /// <returns>A <see cref="T:System.Web.UI.WebControls.Unit" /> of type <see cref="F:System.Web.UI.WebControls.UnitType.Point" /> that represents the length specified by the 32-bit signed integer.</returns>
    public static Unit Point(int n) => new(n, UnitType.Point);

    /// <summary>Converts a <see cref="T:WebFormsCore.UI.WebControls.Unit" /> to a <see cref="T:System.String" />.</summary>
    /// <returns>A <see cref="T:System.String" /> that represents this <see cref="T:WebFormsCore.UI.WebControls.Unit" />.</returns>
    public override string ToString() => ToString((IFormatProvider)CultureInfo.CurrentCulture);

    /// <summary>Converts a <see cref="T:WebFormsCore.UI.WebControls.Unit" /> to a string equivalent in the specified culture.</summary>
    /// <param name="culture">A <see cref="T:System.Globalization.CultureInfo" /> that represents the culture. </param>
    /// <returns>A <see cref="T:System.String" /> represents this <see cref="T:WebFormsCore.UI.WebControls.Unit" /> in the culture specified by <paramref name="culture" />.</returns>
    public string ToString(CultureInfo culture) => ToString((IFormatProvider)culture);

    /// <summary>Converts a <see cref="T:WebFormsCore.UI.WebControls.Unit" /> to a string equivalent using the specified format provider.</summary>
    /// <param name="formatProvider">An <see cref="T:System.IFormatProvider" /> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A <see cref="T:System.String" /> representing this <see cref="T:WebFormsCore.UI.WebControls.Unit" /> in the format specified by <paramref name="formatProvider" />.</returns>
    public string ToString(IFormatProvider formatProvider) => IsEmpty
        ? string.Empty
        : (_type != UnitType.Pixel ? ((float)_value).ToString(formatProvider) : ((int)_value).ToString(formatProvider)) +
          GetStringFromType(_type);

    /// <summary>Implicitly creates a <see cref="T:System.Web.UI.WebControls.Unit" /> of type <see cref="F:System.Web.UI.WebControls.UnitType.Pixel" /> from the specified 32-bit unsigned integer.</summary>
    /// <param name="n">A 32-bit signed integer that represents the length of the <see cref="T:System.Web.UI.WebControls.Unit" />. </param>
    /// <returns>A <see cref="T:System.Web.UI.WebControls.Unit" /> of type <see cref="F:System.Web.UI.WebControls.UnitType.Pixel" /> that represents the 32-bit unsigned integer specified by the <paramref name="n" /> parameter.</returns>
    public static implicit operator Unit(int n) => Pixel(n);
}
