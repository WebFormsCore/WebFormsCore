#if NETFRAMEWORK
using System;
using System.Text;

namespace WebFormsCore;

internal static class EncodingExtensions
{
    public static unsafe string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
    {
        fixed (byte* bytesPtr = bytes)
        {
            return encoding.GetString(bytesPtr, bytes.Length);
        }
    }

    public static unsafe int GetBytes(this Encoding encoding, string s, Span<byte> bytes)
    {
        fixed (char* chars = s)
        fixed (byte* bytesPtr = bytes)
        {
            return encoding.GetBytes(chars, s.Length, bytesPtr, bytes.Length);
        }
    }

    public static unsafe int GetChars(this Encoding encoding, ReadOnlySpan<byte> bytes, Span<char> chars)
    {
        fixed (byte* bytesPtr = bytes)
        fixed (char* charsPtr = chars)
        {
            return encoding.GetChars(bytesPtr, bytes.Length, charsPtr, chars.Length);
        }
    }
}
#endif
