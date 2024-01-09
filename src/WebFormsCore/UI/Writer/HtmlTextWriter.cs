using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WebFormsCore.UI;

public abstract class HtmlTextWriter : IAsyncDisposable
{
    public const int DefaultBufferSize = 4096;

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
    private int _charPos;
    private char[] _charBuffer = ArrayPool<char>.Shared.Rent(DefaultBufferSize);

    public int Position => _charPos;

    protected abstract bool ForceAsync { get; }

    internal bool HasPendingCharacters => _charPos > 0;

    protected abstract void Flush(ReadOnlySpan<char> buffer);

    protected abstract ValueTask FlushAsync(ReadOnlyMemory<char> buffer);

    public void Flush(bool ignoreAsync = false)
    {
        var pos = _charPos;

        if (pos == 0)
        {
            return;
        }

        if (ForceAsync && !ignoreAsync)
        {
            // Instead of flushing the buffer, we grow the buffer and let the async flush handle it.
            GrowBuffer();
            return;
        }

        Flush(_charBuffer.AsSpan(0, pos));
        _charPos = 0;
    }

    private void GrowBuffer()
    {
        var size = _charBuffer.Length * 2;
        var newBuffer = ArrayPool<char>.Shared.Rent(size);
        _charBuffer.AsSpan(0, _charPos).CopyTo(newBuffer);
        ArrayPool<char>.Shared.Return(_charBuffer);
        _charBuffer = newBuffer;

        OnBufferSizeChange(size);
    }

    protected virtual void OnBufferSizeChange(int size)
    {
    }

    public ValueTask FlushAsync()
    {
        var pos = _charPos;

        if (pos == 0)
        {
            return default;
        }

        _charPos = 0;
        return FlushAsync(_charBuffer.AsMemory(0, pos));
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
        if (buffer.Length >= _charBuffer.Length - _charPos)
        {
            WriteSlow(buffer);
            return;
        }

        buffer.CopyTo(_charBuffer.AsSpan(_charPos));
        _charPos += buffer.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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
            }
        }
    }

    public ValueTask WriteAsync(char tagRightChar)
    {
        _charBuffer[_charPos++] = tagRightChar;
        return _charPos == _charBuffer.Length ? FlushAsync() : default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteAsync(string? buffer)
    {
        return buffer is null ? default : WriteAsync(buffer.AsMemory());
    }

    public ValueTask WriteAsync(ReadOnlyMemory<char> buffer)
    {
        if (buffer.Length >= _charBuffer.Length - _charPos)
        {
            return WriteAsyncSlow(buffer);
        }

        buffer.Span.CopyTo(_charBuffer.AsSpan(_charPos));
        _charPos += buffer.Length;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    async ValueTask WriteAsyncSlow(ReadOnlyMemory<char> buffer)
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
        if (size > 4096) size = Encoding.GetCharCount(buffer.Span);

        using var charBuffer = MemoryPool<char>.Shared.Rent(size);

        var memory = charBuffer.Memory;
        var count = Encoding.GetChars(buffer.Span, memory.Span);

        await WriteAsync(memory.Slice(0, count));
    }

    public virtual void Write(ReadOnlySpan<byte> buffer)
    {
        var size = Encoding.GetMaxCharCount(buffer.Length);
        if (size > 4096) size = Encoding.GetCharCount(buffer);

        using var charBuffer = MemoryPool<char>.Shared.Rent(size);

        var span = charBuffer.Memory.Span;
        var count = Encoding.GetChars(buffer, span);

        Write(span.Slice(0, count));
    }

    public virtual ValueTask WriteObjectAsync<T>(T value, bool encode = true)
    {
        var str = value?.ToString();
        if (encode) str = WebUtility.HtmlEncode(str);
        return WriteAsync(str.AsMemory());
    }

    public virtual void WriteObject<T>(T value, bool encode = true)
    {
        var str = value?.ToString();
        if (encode) str = WebUtility.HtmlEncode(str);
        Write(str.AsSpan());
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

    public void AddAttribute(HtmlTextWriterAttribute key, string? value) => AddAttribute(key, value, true);

    public void AddAttribute(HtmlTextWriterAttribute key, string? value, bool encode) => AddAttribute(key.ToName(), value, encode);

    public void AddAttribute(string name, string? value) => AddAttribute(name, value, true);

    public void AddAttribute(string name, string? value, bool encode)
    {
        if (encode) value = WebUtility.HtmlEncode(value);
        _attributes.Add(new KeyValuePair<string, string?>(name, value));
    }

    public void RemoveAttributes(HtmlTextWriterAttribute key) => RemoveAttributes(key.ToName());

    public void RemoveAttributes(string name) => _attributes.RemoveAll(x => x.Key == name);

    public void MergeAttribute(HtmlTextWriterAttribute key, string? value) => MergeAttribute(key, value, true);

    public void MergeAttribute(HtmlTextWriterAttribute key, string? value, bool encode) => MergeAttribute(key.ToName(), value, encode);

    public void MergeAttribute(string name, string? value) => MergeAttribute(name, value, true);

    public void MergeAttribute(string name, string? value, bool encode)
    {
        if (encode) value = WebUtility.HtmlEncode(value);
        var index = _attributes.FindIndex(x => x.Key == name);
        if (index == -1)
        {
            _attributes.Add(new KeyValuePair<string, string?>(name, value));
        }
        else
        {
            var oldValue = _attributes[index].Value;
            var separator = name.Equals("style", StringComparison.OrdinalIgnoreCase) ? ';' : ' ';

            _attributes[index] = new KeyValuePair<string, string?>(name, $"{oldValue}{separator}{value}");
        }
    }

    public void AddStyleAttribute(HtmlTextWriterStyle key, string? value) => AddStyleAttribute(key, value, true);

    public void AddStyleAttribute(HtmlTextWriterStyle key, string? value, bool encode) => AddStyleAttribute(key.ToName(), value, encode);

    public void AddStyleAttribute(string name, string? value) => AddStyleAttribute(name, value, true);

    public void AddStyleAttribute(string name, string? value, bool encode)
    {
        if (encode) value = WebUtility.HtmlEncode(value);
        _styleAttributes.Add(new KeyValuePair<string, string?>(name, value));
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        await FlushAsync();
        ArrayPool<char>.Shared.Return(_charBuffer);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }
}
