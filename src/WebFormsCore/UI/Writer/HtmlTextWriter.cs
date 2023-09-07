using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI;

public class StringHtmlTextWriter : HtmlTextWriter
{
    private readonly StringBuilder _stringBuilder = new();

#if NET
    protected override void Flush(ReadOnlySpan<char> buffer)
    {
        _stringBuilder.Append(buffer);
    }

    protected override ValueTask FlushAsync(ReadOnlyMemory<char> buffer)
    {
        _stringBuilder.Append(buffer.Span);
        return default;
    }
#else
    protected override unsafe void Flush(ReadOnlySpan<char> buffer)
    {
        fixed (char* ptr = buffer)
        {
            _stringBuilder.Append(ptr, buffer.Length);
        }
    }

    protected override ValueTask FlushAsync(ReadOnlyMemory<char> buffer)
    {
        Flush(buffer.Span);
        return default;
    }
#endif

    public override string ToString()
    {
        return _stringBuilder.ToString();
    }
}

public class StreamHtmlTextWriter : HtmlTextWriter
{
    private readonly Stream _stream;
    private readonly byte[] _buffer;

    public StreamHtmlTextWriter(Stream stream)
    {
        _stream = stream;
        _buffer = ArrayPool<byte>.Shared.Rent(Encoding.GetMaxByteCount(BufferSize));
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
}

public abstract class HtmlTextWriter : IDisposable
{
    public const int BufferSize = 1024;

    public const string DefaultTabString = "\t";
    public const char DoubleQuoteChar = '"';
    public const string EndTagLeftChars = "</";
    public const char EqualsChar = '=';
    public const string EqualsDoubleQuoteString = "=\"";
    public const string SelfClosingChars = " /";
    public const string SelfClosingTagEnd = " />";
    public const char SemicolonChar = ';';
    public const char SingleQuoteChar = '\'';
    public const char SlashChar = '/';
    public const char SpaceChar = ' ';
    public const char StyleEqualsChar = ':';
    public const char TagLeftChar = '<';
    public const char TagRightChar = '>';
    public const string StyleDeclaringString = "style";

    protected readonly Encoding Encoding = Encoding.UTF8;
    private readonly Stack<string> _openTags = new();
    private readonly List<KeyValuePair<string, string?>> _attributes = new();
    private readonly List<KeyValuePair<string, string?>> _styleAttributes = new();

    public void AddAttribute(HtmlTextWriterAttribute key, string? value) => AddAttribute(key, value, true);

    public void AddAttribute(HtmlTextWriterAttribute key, string? value, bool encode) =>
        AddAttribute(key.ToName(), value, encode);

    public void AddAttribute(string name, string? value) => AddAttribute(name, value, true);

    public void AddAttribute(string name, string? value, bool encode)
    {
        if (encode) value = WebUtility.HtmlEncode(value);

        _attributes.Add(new KeyValuePair<string, string?>(name, value));
    }

    public void RemoveAttributes(HtmlTextWriterAttribute key) => RemoveAttributes(key.ToName());

    public void RemoveAttributes(string name)
    {
        _attributes.RemoveAll(x => x.Key == name);
    }

    public void AddStyleAttribute(HtmlTextWriterStyle key, string? value) => AddStyleAttribute(key, value, true);

    public void AddStyleAttribute(HtmlTextWriterStyle key, string? value, bool encode) =>
        AddStyleAttribute(key.ToName(), value, encode);

    public void AddStyleAttribute(string name, string? value) => AddStyleAttribute(name, value, true);

    public void AddStyleAttribute(string name, string? value, bool encode)
    {
        if (encode)
            value = WebUtility.HtmlEncode(value);

        _styleAttributes.Add(new KeyValuePair<string, string?>(name, value));
    }

    private int _charPos;
    private readonly char[] _charBuffer = ArrayPool<char>.Shared.Rent(1024);

    protected abstract void Flush(ReadOnlySpan<char> buffer);

    protected abstract ValueTask FlushAsync(ReadOnlyMemory<char> buffer);

    public void Flush()
    {
        if (_charPos == 0) return;
        Flush(_charBuffer.AsSpan(0, _charPos));
        _charPos = 0;
    }

    public async ValueTask FlushAsync()
    {
        if (_charPos == 0) return;
        await FlushAsync(_charBuffer.AsMemory(0, _charPos));
        _charPos = 0;
    }

    public void Write(char tagRightChar)
    {
        _charBuffer[_charPos++] = tagRightChar;
        if (_charPos == _charBuffer.Length) Flush();
    }

    private void Write(string? buffer)
    {
        if (buffer is null) return;
        Write(buffer.AsSpan());
    }

    public void Write(ReadOnlySpan<char> buffer)
    {
        var remaining = (BufferSize - 1) - _charPos;

        if (buffer.Length > remaining)
        {
            WriteSlow(buffer);
            return;
        }

        buffer.CopyTo(_charBuffer.AsSpan(_charPos));
        _charPos += buffer.Length;
    }

    private void WriteSlow(ReadOnlySpan<char> buffer)
    {
        while (buffer.Length > 0)
        {
            var count = Math.Min(buffer.Length, _charBuffer.Length - _charPos);
            buffer.Slice(0, count).CopyTo(_charBuffer.AsSpan(_charPos));
            _charPos += count;
            buffer = buffer.Slice(count);

            if (_charPos == _charBuffer.Length)
            {
                Flush();
                _charPos = 0;
            }
        }
    }

    public ValueTask WriteAsync(char tagRightChar)
    {
        _charBuffer[_charPos++] = tagRightChar;
        return _charPos == _charBuffer.Length ? FlushAsync() : default;
    }

    public ValueTask WriteAsync(string? buffer)
    {
        if (buffer is null) return default;
        return WriteAsync(buffer.AsMemory());
    }

    public ValueTask WriteAsync(ReadOnlyMemory<char> buffer)
    {
        var remaining = (BufferSize - 1) - _charPos;

        if (buffer.Length > remaining)
        {
            return WriteAsyncSlow(buffer);
        }

        buffer.Span.CopyTo(_charBuffer.AsSpan(_charPos));
        _charPos += buffer.Length;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async ValueTask WriteAsyncSlow(ReadOnlyMemory<char> buffer)
    {
        while (buffer.Length > 0)
        {
            var count = Math.Min(buffer.Length, _charBuffer.Length - _charPos);
            buffer.Slice(0, count).CopyTo(_charBuffer.AsMemory(_charPos));
            _charPos += count;
            buffer = buffer.Slice(count);

            if (_charPos == _charBuffer.Length)
            {
                await FlushAsync(_charBuffer.AsMemory(0, _charPos));
                _charPos = 0;
            }
        }
    }

    public virtual async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer)
    {
        var size = Encoding.GetMaxCharCount(buffer.Length);
        using var charBuffer = MemoryPool<char>.Shared.Rent(size);

        var chars = charBuffer.Memory.Slice(0, size);
        var count = Encoding.GetChars(buffer.Span, chars.Span);

        await WriteAsync(chars.Slice(0, count));
    }

    public virtual ValueTask WriteObjectAsync(object? value)
    {
        return WriteAsync(value?.ToString());
    }

    public virtual void WriteObject(object? value)
    {
        Write(value?.ToString());
    }

    public void RenderBeginTag(HtmlTextWriterTag tagKey) => RenderBeginTag(tagKey.ToName());

    public void RenderBeginTag(string name)
    {
        WriteBeginTag(name);

        if (_attributes.Count > 0)
        {
            foreach (var attribute in _attributes)
                WriteAttribute(attribute.Key, attribute.Value, false); // Already encoded

            _attributes.Clear();
        }

        if (_styleAttributes.Count > 0)
        {
            Write(SpaceChar);
            Write(StyleDeclaringString);
            Write(EqualsDoubleQuoteString);

            foreach (var styleAttribute in _styleAttributes)
            {
                WriteStyleAttribute(styleAttribute.Key, styleAttribute.Value, false);
            }

            Write(DoubleQuoteChar);
            _styleAttributes.Clear();
        }

        Write(TagRightChar);
        _openTags.Push(name);
    }

    public ValueTask RenderBeginTagAsync(HtmlTextWriterTag tagKey) => RenderBeginTagAsync(tagKey.ToName());

    public ValueTask RenderSelfClosingTagAsync(HtmlTextWriterTag tagKey) => RenderSelfClosingTagAsync(tagKey.ToName());

    public async ValueTask RenderBeginTagAsync(string name)
    {
        await WriteBeginTagAsync(name);
        await RenderAttributesAsync();

        _openTags.Push(name);
        await WriteAsync(TagRightChar);
    }

    public async ValueTask RenderSelfClosingTagAsync(string name)
    {
        await WriteBeginTagAsync(name);
        await RenderAttributesAsync();
        await WriteAsync(SelfClosingTagEnd);
    }

    private async ValueTask RenderAttributesAsync()
    {
        if (_attributes.Count > 0)
        {
            foreach (var attribute in _attributes)
            {
                await WriteAttributeAsync(attribute.Key, attribute.Value, false); // Already encoded
            }

            _attributes.Clear();
        }

        if (_styleAttributes.Count > 0)
        {
            await WriteAsync(SpaceChar);
            await WriteAsync(StyleDeclaringString);
            await WriteAsync(EqualsDoubleQuoteString);

            foreach (var styleAttribute in _styleAttributes)
            {
                await WriteStyleAttributeAsync(styleAttribute.Key, styleAttribute.Value, false);
            }

            await WriteAsync(DoubleQuoteChar);
            _styleAttributes.Clear();
        }
    }

    public void RenderEndTag()
    {
        if (_openTags == null || !_openTags.Any())
            throw new InvalidOperationException();

        var tag = _openTags.Pop();
        Write(EndTagLeftChars);
        Write(tag);
        Write(TagRightChar);
        Write('\n');
    }

    public async ValueTask RenderEndTagAsync()
    {
        if (_openTags == null || !_openTags.Any())
            throw new InvalidOperationException();

        var tag = _openTags.Pop();
        await WriteAsync(EndTagLeftChars);
        await WriteAsync(tag);
        await WriteAsync(TagRightChar);
        await WriteAsync('\n');
    }

    public void WriteAttribute(string name, string? value, bool encode = true)
    {
        Write(SpaceChar);
        Write(name);

        if (value == null) return;
        if (encode) value = WebUtility.HtmlEncode(value);

        Write(EqualsDoubleQuoteString);
        Write(value);
        Write(DoubleQuoteChar);
    }

    public async ValueTask WriteAttributeAsync(string name, string? value, bool encode = true)
    {
        await WriteAsync(SpaceChar);
        await WriteAsync(name);

        if (value == null) return;
        if (encode) value = WebUtility.HtmlEncode(value);

        await WriteAsync(EqualsDoubleQuoteString);
        await WriteAsync(value);
        await WriteAsync(DoubleQuoteChar);
    }

    public void WriteStyleAttribute(string? name, string? value, bool encode = true)
    {
        if (name == null || value == null)
        {
            return;
        }

        Write(name);
        Write(StyleEqualsChar);
        if (encode) value = WebUtility.HtmlEncode(value);

        Write(value);
        Write(SemicolonChar);
        Write(SpaceChar);
    }

    public async ValueTask WriteStyleAttributeAsync(string? name, string? value, bool encode = true)
    {
        if (name == null || value == null)
        {
            return;
        }

        await WriteAsync(name);
        await WriteAsync(StyleEqualsChar);
        if (encode) value = WebUtility.HtmlEncode(value);

        await WriteAsync(value);
        await WriteAsync(SemicolonChar);
        await WriteAsync(SpaceChar);
    }

    public void WriteBeginTag(string name)
    {
        Write(TagLeftChar);
        Write(name);
    }

    public async ValueTask WriteBeginTagAsync(string name)
    {
        await WriteAsync(TagLeftChar);
        await WriteAsync(name);
    }

    public void WriteBreak()
    {
        Write(TagLeftChar);
        Write("br");
        Write(SelfClosingTagEnd);
    }

    public async ValueTask WriteBreakAsync()
    {
        await WriteAsync(TagLeftChar);
        await WriteAsync("br");
        await WriteAsync(SelfClosingTagEnd);
    }

    public async ValueTask WriteSelfClosingAsync()
    {
        await WriteAsync(SelfClosingTagEnd);
    }

    public void WriteEncodedText(string text)
    {
        Write(WebUtility.HtmlEncode(text));
    }

    public async ValueTask WriteEncodedTextAsync(string? text)
    {
        await WriteAsync(WebUtility.HtmlEncode(text));
    }

    public void WriteEncodedUrl(string url)
    {
        var index = url.IndexOf('?');
        if (index != -1)
        {
            Write(Uri.EscapeDataString(url.Substring(0, index)));
            Write(url.Substring(index));
        }
        else
        {
            Write(Uri.EscapeDataString(url));
        }
    }

    public void WriteEncodedUrlParameter(string urlText)
    {
        Write(Uri.EscapeDataString(urlText));
    }

    public void WriteEndTag(string tagName)
    {
        Write(TagLeftChar);
        Write(SlashChar);
        Write(tagName);
        Write(TagRightChar);
    }

    public async ValueTask WriteEndTagAsync(string tagName)
    {
        await WriteAsync(TagLeftChar);
        await WriteAsync(SlashChar);
        await WriteAsync(tagName);
        await WriteAsync(TagRightChar);
    }

    public ValueTask WriteLineAsync()
    {
        return WriteAsync("\n");
    }

    public void Dispose()
    {
        Dispose(true);
    }

    ~HtmlTextWriter()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Flush();
            ArrayPool<char>.Shared.Return(_charBuffer);
        }
    }
}
