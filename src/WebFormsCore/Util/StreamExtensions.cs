using System;
using System.IO;

#if NETFRAMEWORK
namespace WebFormsCore;

internal static class StreamExtensions
{
    public static unsafe int Read(this Stream stream, Span<byte> span)
    {
        fixed (byte* pSource = &span[0])
        {
            using var sourceStream = new UnmanagedMemoryStream(pSource, span.Length);
            sourceStream.CopyTo(stream);
            return (int)stream.Position;
        }
    }

    public static unsafe void Write(this Stream stream, ReadOnlySpan<byte> span)
    {
        fixed (byte* pDestination = &span[0])
        {
            using var destinationStream = new UnmanagedMemoryStream(pDestination, span.Length, span.Length, FileAccess.Write);
            stream.CopyTo(destinationStream);
        }
    }
}
#endif
