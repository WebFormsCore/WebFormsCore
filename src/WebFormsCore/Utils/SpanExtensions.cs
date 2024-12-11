using System;
using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;

#if NETSTANDARD2_0 || NETFRAMEWORK
namespace WebFormsCore;

internal static class SpanExtensions
{
    public static bool Contains(this ReadOnlySpan<char> span, char value)
    {
        return span.IndexOf(value) != -1;
    }
}
#endif
