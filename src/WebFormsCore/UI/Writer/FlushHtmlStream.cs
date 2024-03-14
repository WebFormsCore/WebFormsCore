using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebFormsCore.UI;

/// <summary>
/// Flushes the HtmlTextWriter before writing to the stream.
/// </summary>
internal class FlushHtmlStream : Stream
{
    private readonly Stream _stream;
    private readonly HtmlTextWriter _writer;

    public FlushHtmlStream(Stream stream, HtmlTextWriter writer)
    {
        _stream = stream;
        _writer = writer;
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _stream.BeginRead(buffer, offset, count, callback, state);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        throw new NotSupportedException();
    }

    public override void Close()
    {
        _stream.Close();
    }

    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        await _writer.FlushAsync();
        await _stream.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return _stream.EndRead(asyncResult);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        _stream.EndWrite(asyncResult);
    }

    public override void Flush()
    {
        // Ignore
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        if (_writer.HasPendingCharacters)
        {
            await _writer.FlushAsync();
        }
        else
        {
            await _stream.FlushAsync(cancellationToken);
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _stream.Read(buffer, offset, count);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _stream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override int ReadByte()
    {
        return _stream.ReadByte();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _stream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _writer.Write(buffer.AsSpan(offset, count));
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await _writer.FlushAsync();
        await _stream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override void WriteByte(byte value)
    {
        throw new NotSupportedException();
    }

#if NET
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        await _writer.FlushAsync();
        await _stream.WriteAsync(buffer, cancellationToken);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _writer.Write(buffer);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        return _stream.ReadAsync(buffer, cancellationToken);
    }

    public override int Read(Span<byte> buffer)
    {
        return _stream.Read(buffer);
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        _stream.CopyTo(destination, bufferSize);
    }

    public override ValueTask DisposeAsync()
    {
        return _stream.DisposeAsync();
    }
#endif

    public override bool CanRead => _stream.CanRead;

    public override bool CanSeek => _stream.CanSeek;

    public override bool CanTimeout => _stream.CanTimeout;

    public override bool CanWrite => _stream.CanWrite;

    public override long Length => _stream.Length;

    public override long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    public override int ReadTimeout
    {
        get => _stream.ReadTimeout;
        set => _stream.ReadTimeout = value;
    }

    public override int WriteTimeout
    {
        get => _stream.WriteTimeout;
        set => _stream.WriteTimeout = value;
    }

    public ValueTask WriteAsync(string content)
    {
        return WriteAsync(content.AsMemory());
    }

    public ValueTask WriteAsync(ReadOnlyMemory<char> content)
    {
        return _writer.WriteAsync(content);
    }

    public void Write(string content)
    {
        _writer.Write(content.AsSpan());
    }

    public void Write(ReadOnlySpan<char> content)
    {
        _writer.Write(content);
    }
}
