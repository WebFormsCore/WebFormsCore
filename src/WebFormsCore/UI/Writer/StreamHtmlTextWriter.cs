using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace WebFormsCore.UI;

public class StreamHtmlTextWriter : HtmlTextWriter
{
    private readonly Stream _stream;
    private byte[] _buffer;

    public StreamHtmlTextWriter(Stream stream)
    {
        _stream = stream;
        _buffer = ArrayPool<byte>.Shared.Rent(Encoding.GetMaxByteCount(DefaultBufferSize));
    }

    protected override bool ForceAsync => true;

    protected override void OnBufferSizeChange(int size)
    {
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = ArrayPool<byte>.Shared.Rent(Encoding.GetMaxByteCount(size));
    }

    protected override void Flush(ReadOnlySpan<char> buffer)
    {
        var length = Encoding.GetBytes(buffer, _buffer);
        _stream.Write(_buffer, 0, length);
        _stream.Flush();
    }

    protected override async ValueTask FlushAsync(ReadOnlyMemory<char> buffer)
    {
        var length = Encoding.GetBytes(buffer.Span, _buffer);
        await _stream.WriteAsync(_buffer, 0, length);
        await _stream.FlushAsync();
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer)
    {
        return HasPendingCharacters ? FlushAndWriteAsync(buffer) : _stream.WriteAsync(buffer);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async ValueTask FlushAndWriteAsync(ReadOnlyMemory<byte> buffer)
    {
        await FlushAsync();
        await _stream.WriteAsync(buffer);
    }

    public override bool TryGetStream([NotNullWhen(true)] out Stream? stream)
    {
        if (HasPendingCharacters)
        {
            stream = null;
            return false;
        }

        stream = _stream;
        return true;
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await base.DisposeAsyncCore();
        ArrayPool<byte>.Shared.Return(_buffer);
    }
}