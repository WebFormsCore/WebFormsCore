#if NETSTANDARD2_0
using System;
using System.Text;

namespace WebFormsCore;

public static class EncodingExtensions
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

    public static unsafe int GetBytes(this Encoding encoding, ReadOnlySpan<char> s, Span<byte> bytes)
    {
        fixed (char* chars = s)
        fixed (byte* bytesPtr = bytes)
        {
            return encoding.GetBytes(chars, s.Length, bytesPtr, bytes.Length);
        }
    }

    public static unsafe int GetByteCount(this Encoding encoding, ReadOnlySpan<char> s)
    {
        fixed (char* chars = s)
        {
            return encoding.GetByteCount(chars, s.Length);
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

    public static unsafe int GetCharCount(this Encoding encoding, ReadOnlySpan<byte> bytes)
    {
        fixed (byte* bytesPtr = bytes)
        {
            return encoding.GetCharCount(bytesPtr, bytes.Length);
        }
    }
}
#endif
