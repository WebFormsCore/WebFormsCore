using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#if NETSTANDARD2_0
namespace WebFormsCore;

internal static class StreamExtensions
{
    public static unsafe int Read(this Stream sourceStream, Span<byte> span)
    {
        fixed (byte* pSource = span)
        {
            using var stream = new UnmanagedMemoryStream(pSource, span.Length, span.Length, FileAccess.Write);
            sourceStream.CopyTo(stream);
            return (int)stream.Position;
        }
    }

    public static unsafe void Write(this Stream destinationStream, ReadOnlySpan<byte> span)
    {
        fixed (byte* pDestination = span)
        {
            using var stream = new UnmanagedMemoryStream(pDestination, span.Length);
            stream.CopyTo(destinationStream);
        }
    }

    public static async Task WriteAsync(this Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken token)
    {
        using var pointer = buffer.Pin();
        using var destinationStream = CreateMemoryStream(buffer, pointer);

        await destinationStream.CopyToAsync(stream, 81920, token);
    }

    private static unsafe UnmanagedMemoryStream CreateMemoryStream(ReadOnlyMemory<byte> buffer, MemoryHandle pointer)
    {
        return new UnmanagedMemoryStream((byte*)pointer.Pointer, buffer.Length);
    }
}
#endif
