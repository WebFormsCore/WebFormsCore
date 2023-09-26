#if NETSTANDARD || NETFRAMEWORK
using System.Text.RegularExpressions;

namespace WebFormsCore;

public static class StringExtensions
{
    private static readonly Regex NewLineRegex = new(@"\r\n|\r|\n", RegexOptions.Compiled);

    public static string ReplaceLineEndings(this string str, string replacement)
    {
        return NewLineRegex.Replace(str, replacement);
    }
}
#endif
