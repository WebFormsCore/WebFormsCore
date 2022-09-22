using System.Buffers.Text;
using System.Text;

namespace System.Web
{
    internal static class EncodingExtensions
    {
#if NETSTANDARD
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

        public static bool StartsWith(this string str, char c)
        {
            return str.Length > 0 && str[0] == c;
        }

        public static bool EndsWith(this string str, char c)
        {
            var length = str.Length;
            return length > 0 && str[length - 1] == c;
        }
#endif

    }
}
