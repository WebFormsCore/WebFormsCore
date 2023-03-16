using System.Text.RegularExpressions;

#if NETFRAMEWORK
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
}
#endif
