using System;
using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;

#if NETSTANDARD2_0 || NETFRAMEWORK
namespace WebFormsCore;

internal static class StringExtensions
{
    private static readonly Regex NewLineRegex = new(@"\r\n|\r|\n", RegexOptions.Compiled);

    public static string ReplaceLineEndings(this string str, string replacement)
    {
        return NewLineRegex.Replace(str, replacement);
    }

    public static bool StartsWith(this string str, char c)
    {
        return str.Length > 0 && str[0] == c;
    }

    public static bool EndsWith(this string str, char c)
    {
        var length = str.Length;
        return length > 0 && str[length - 1] == c;
    }

    public static StringBuilder Append(this StringBuilder sb, ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return sb;
        }

        if (value.Length <= 3)
        {
            foreach (var c in value)
            {
                sb.Append(c);
            }
        }
        else
        {
            var array = ArrayPool<char>.Shared.Rent(value.Length);
            value.CopyTo(array);
            sb.Append(array, 0, value.Length);
            ArrayPool<char>.Shared.Return(array);
        }

        return sb;
    }
}
#endif
