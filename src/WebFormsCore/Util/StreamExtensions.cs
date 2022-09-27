using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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

    public static async Task WriteAsync(this Stream stream, Memory<byte> buffer, CancellationToken token)
    {
        using var pointer = buffer.Pin();
        using var destinationStream = CreateMemoryStream(buffer, pointer);

        await destinationStream.CopyToAsync(stream, 81920, token);
    }

    private static unsafe UnmanagedMemoryStream CreateMemoryStream(Memory<byte> buffer, MemoryHandle pointer)
    {
        return new UnmanagedMemoryStream((byte*)pointer.Pointer, buffer.Length, buffer.Length, FileAccess.Write);
    }
}
#endif
