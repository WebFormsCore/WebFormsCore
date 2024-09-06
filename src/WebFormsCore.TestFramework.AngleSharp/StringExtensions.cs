namespace WebFormsCore.TestFramework.AngleSharp;

internal static class StringExtensions
{
    public static bool Is(this string? value, string? other)
    {
        return string.Equals(value, other, StringComparison.OrdinalIgnoreCase);
    }

    public static bool Is(this string? value, string? s1, string? s2)
    {
        return string.Equals(value, s1, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, s2, StringComparison.OrdinalIgnoreCase);
    }

    public static bool Is(this string? value, string? s1, string? s2, string? s3)
    {
        return string.Equals(value, s1, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, s2, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, s3, StringComparison.OrdinalIgnoreCase);
    }

    public static bool Is(this string? value, ReadOnlySpan<string> values)
    {
        foreach (var s in values)
        {
            if (string.Equals(value, s, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
