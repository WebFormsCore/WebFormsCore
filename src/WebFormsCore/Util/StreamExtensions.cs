using System;
using System.Buffers;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

#if NETSTANDARD2_0 || NETFRAMEWORK
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

    public static async ValueTask<WebSocketReceiveResult> ReceiveAsync(this WebSocket socket, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> arraySegment))
        {
            return await socket.ReceiveAsync(arraySegment, cancellationToken).ConfigureAwait(false);
        }

        var array = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            return await socket.ReceiveAsync(new ArraySegment<byte>(array, 0, buffer.Length), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    public static async ValueTask WriteAsync(this Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken token = default)
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
