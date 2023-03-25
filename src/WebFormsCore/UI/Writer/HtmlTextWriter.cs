#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI;

public class HtmlTextWriter : TextWriter
#if NETFRAMEWORK
    , IAsyncDisposable
#endif
{
#if NETFRAMEWORK
    private const bool DefaultAsync = false;
#else
    private const bool DefaultAsync = true;
#endif

    private static readonly byte[] NewLineLf = Encoding.UTF8.GetBytes("\n");

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

    private readonly Stack<string> _openTags = new();
    private readonly byte[] _newLineBytes;
    private readonly List<KeyValuePair<string, string?>> _attributes = new();
    private readonly List<KeyValuePair<string, string?>> _styleAttributes = new();

    private TextWriter? _innerWriter;
    private bool _disposeInnerWriter;
    private readonly Stream? _stream;

    internal void Initialize(TextWriter writer)
    {
        _innerWriter = writer;
    }

    public bool UseAsync { get; set; } = DefaultAsync;

    public bool AutoFlush { get; set; } = true;

    internal void Clear()
    {
        _openTags.Clear();
        _attributes.Clear();
        _styleAttributes.Clear();
        _innerWriter = null;
    }

    public HtmlTextWriter(TextWriter writer, Stream? stream = null)
    {
        _stream = stream;
        _innerWriter = writer;

        if (writer.Encoding.WebName == "utf-8" && writer.NewLine == "\n")
        {
            _newLineBytes = NewLineLf;
        }
        else
        {
            _newLineBytes = writer.Encoding.GetBytes(writer.NewLine);
        }
    }

    public HtmlTextWriter()
        : this(new StringWriter { NewLine = "\n" })
    {
        _disposeInnerWriter = true;
    }

    public TextWriter InnerWriter
    {
        get
        {
            Debug.Assert(_innerWriter != null);
            return _innerWriter!;
        }
    }

    public override string NewLine => InnerWriter.NewLine;

    public override Encoding Encoding => InnerWriter.Encoding;

    public void AddAttribute(HtmlTextWriterAttribute key, string? value) => AddAttribute(key, value, true);

    public void AddAttribute(HtmlTextWriterAttribute key, string? value, bool encode) =>
        AddAttribute(key.ToName(), value, encode);

    public void AddAttribute(string name, string? value) => AddAttribute(name, value, true);

    public void AddAttribute(string name, string? value, bool encode)
    {
        if (encode) value = WebUtility.HtmlEncode(value);

        _attributes.Add(new KeyValuePair<string, string?>(name, value));
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

    //
    // Tags
    //
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

        InnerWriter.Write(TagRightChar);
        if (AutoFlush) Flush();

        _openTags.Push(name);
    }

    public ValueTask RenderBeginTagAsync(HtmlTextWriterTag tagKey) => RenderBeginTagAsync(tagKey.ToName());

    public async ValueTask RenderBeginTagAsync(string name)
    {
        await WriteBeginTagAsync(name);

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

        _openTags.Push(name);
        await WriteAsync(TagRightChar);
        if (AutoFlush) await FlushAsync();
    }

    public void RenderEndTag()
    {
        if (_openTags == null || !_openTags.Any())
            throw new InvalidOperationException();

        var tag = _openTags.Pop();
        Write(EndTagLeftChars);
        Write(tag);
        WriteLine(TagRightChar);
        if (AutoFlush) Flush();
    }

    public async ValueTask RenderEndTagAsync()
    {
        if (_openTags == null || !_openTags.Any())
            throw new InvalidOperationException();

        var tag = _openTags.Pop();
        await WriteAsync(EndTagLeftChars);
        await WriteAsync(tag);
        await WriteLineAsync(TagRightChar);
        if (AutoFlush) await FlushAsync();
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
        if (AutoFlush) Flush();
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
        if (AutoFlush) await FlushAsync();
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
        if (AutoFlush) Flush();
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
        if (AutoFlush) await FlushAsync();
    }

    public void WriteBeginTag(string name)
    {
        Write(TagLeftChar);
        Write(name);
        if (AutoFlush) Flush();
    }

    public async ValueTask WriteBeginTagAsync(string name)
    {
        await WriteAsync(TagLeftChar);
        await WriteAsync(name);
        if (AutoFlush) await FlushAsync();
    }

    public void WriteBreak()
    {
        Write(TagLeftChar);
        Write("br");
        Write(SelfClosingTagEnd);
        if (AutoFlush) Flush();
    }

    public async ValueTask WriteBreakAsync()
    {
        await WriteAsync(TagLeftChar);
        await WriteAsync("br");
        await WriteAsync(SelfClosingTagEnd);
        if (AutoFlush)  await FlushAsync();
    }

    public async ValueTask WriteSelfClosingAsync()
    {
        await WriteAsync(SelfClosingTagEnd);
        if (AutoFlush) await FlushAsync();
    }

    public void WriteEncodedText(string text)
    {
        Write(WebUtility.HtmlEncode(text));
        if (AutoFlush) Flush();
    }

    public async Task WriteEncodedTextAsync(string? text)
    {
        await WriteAsync(WebUtility.HtmlEncode(text));
        if (AutoFlush) await FlushAsync();
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

        if (AutoFlush) Flush();
    }

    public void WriteEncodedUrlParameter(string urlText)
    {
        Write(Uri.EscapeDataString(urlText));
        if (AutoFlush) Flush();
    }

    public void WriteEndTag(string tagName)
    {
        Write(TagLeftChar);
        Write(SlashChar);
        Write(tagName);
        Write(TagRightChar);
        if (AutoFlush) Flush();
    }

    public async ValueTask WriteEndTagAsync(string tagName)
    {
        await WriteAsync(TagLeftChar);
        await WriteAsync(SlashChar);
        await WriteAsync(tagName);
        await WriteAsync(TagRightChar);
        if (AutoFlush) await FlushAsync();
    }

    public void WriteFullBeginTag(string tagName) => Write($"{TagLeftChar}{tagName}{TagRightChar}");
    public void WriteLineNoTabs(string line) => WriteLine(line);

    //
    // Close/Flush
    //

#if NETSTANDARD1_4
        public void Close() { }
#else
    public override void Close() => InnerWriter.Close();
#endif
    public override void Flush() => InnerWriter.Flush();
    public override Task FlushAsync() => InnerWriter.FlushAsync();

    //
    // Write
    //

    public override void Write(ulong value)
    {
        InnerWriter.Write(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(uint value)
    {
        InnerWriter.Write(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(string format, params object?[] arg)
    {
        InnerWriter.Write(format, arg);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(string format, object? arg0, object? arg1, object? arg2)
    {
        InnerWriter.Write(format, arg0, arg1, arg2);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(string format, object? arg0, object? arg1)
    {
        InnerWriter.Write(format, arg0, arg1);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(string format, object? arg0)
    {
        InnerWriter.Write(format, arg0);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(string? value)
    {
        InnerWriter.Write(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(object? value)
    {
        InnerWriter.Write(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(long value)
    {
        InnerWriter.Write(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(int value)
    {
        InnerWriter.Write(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(double value)
    {
        InnerWriter.Write(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(decimal value)
    {
        InnerWriter.Write(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(char[] buffer, int index, int count)
    {
        InnerWriter.Write(buffer, index, count);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(char[]? buffer)
    {
        InnerWriter.Write(buffer);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(char value)
    {
        InnerWriter.Write(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(bool value)
    {
        InnerWriter.Write(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(float value)
    {
        InnerWriter.Write(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public async Task WriteObjectAsync(object? value)
    {
        if (value == null) return;

        await WriteAsync(value.ToString());
        if (AutoFlush) await FlushAsync();
    }

    public override async Task WriteAsync(string? value)
    {
        if (UseAsync)
        {
            await InnerWriter.WriteAsync(value);
            if (AutoFlush) await InnerWriter.FlushAsync();
            return;
        }

        // ReSharper disable once MethodHasAsyncOverload
        InnerWriter.Write(value);
        // ReSharper disable once MethodHasAsyncOverload
        if (AutoFlush) InnerWriter.Flush();
    }

    public override async Task WriteAsync(char value)
    {
        if (UseAsync)
        {
            await InnerWriter.WriteAsync(value);
            if (AutoFlush) await InnerWriter.FlushAsync();
            return;
        }

        // ReSharper disable once MethodHasAsyncOverload
        InnerWriter.Write(value);
        // ReSharper disable once MethodHasAsyncOverload
        if (AutoFlush) InnerWriter.Flush();
    }

#if NET
    public override async Task WriteAsync(StringBuilder? value,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await InnerWriter.WriteAsync(value, cancellationToken);
        if (AutoFlush) await InnerWriter.FlushAsync();
    }

    public override async Task WriteAsync(ReadOnlyMemory<char> buffer,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await InnerWriter.WriteAsync(buffer, cancellationToken);
        if (AutoFlush) await InnerWriter.FlushAsync();
    }

    public override async Task WriteLineAsync(ReadOnlyMemory<char> buffer,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await InnerWriter.WriteAsync(buffer, cancellationToken);
        if (AutoFlush) await InnerWriter.FlushAsync();
    }

    public override void Write(ReadOnlySpan<char> buffer)
    {
        InnerWriter.Write(buffer);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void Write(StringBuilder? value)
    {
        InnerWriter.Write(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(ReadOnlySpan<char> buffer)
    {
        InnerWriter.WriteLine(buffer);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(StringBuilder? value)
    {
        InnerWriter.WriteLine(value);
        if (AutoFlush)  InnerWriter.Flush();
    }

    public override async Task WriteLineAsync(StringBuilder? value,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await InnerWriter.WriteLineAsync(value, cancellationToken);
        if (AutoFlush) await InnerWriter.FlushAsync();
    }
#endif

    public override async Task WriteAsync(char[] buffer, int index, int count)
    {
        if (UseAsync)
        {
            await InnerWriter.WriteAsync(buffer, index, count);
            if (AutoFlush) await InnerWriter.FlushAsync();
        }

        // ReSharper disable once MethodHasAsyncOverload
        InnerWriter.Write(buffer, index, count);
        // ReSharper disable once MethodHasAsyncOverload
        if (AutoFlush)  InnerWriter.Flush();
    }

    //
    // WriteLine
    //

    public override void WriteLine(string format, object? arg0)
    {
        InnerWriter.WriteLine(format, arg0);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(ulong value)
    {
        InnerWriter.WriteLine(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(uint value)
    {
        InnerWriter.WriteLine(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(string format, params object?[] arg)
    {
        InnerWriter.WriteLine(format, arg);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
    {
        InnerWriter.WriteLine(format, arg0, arg1, arg2);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(string format, object? arg0, object? arg1)
    {
        InnerWriter.WriteLine(format, arg0, arg1);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(string? value)
    {
        InnerWriter.WriteLine(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(float value)
    {
        InnerWriter.WriteLine(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine()
    {
        InnerWriter.WriteLine();
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(long value)
    {
        InnerWriter.WriteLine(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(int value)
    {
        InnerWriter.WriteLine(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(double value)
    {
        InnerWriter.WriteLine(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(decimal value)
    {
        InnerWriter.WriteLine(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        InnerWriter.WriteLine(buffer, index, count);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(char[]? buffer)
    {
        InnerWriter.WriteLine(buffer);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(char value)
    {
        InnerWriter.WriteLine(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(bool value)
    {
        InnerWriter.WriteLine(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override void WriteLine(object? value)
    {
        InnerWriter.WriteLine(value);
        if (AutoFlush) InnerWriter.Flush();
    }

    public override async Task WriteLineAsync()
    {
        if (UseAsync)
        {
            await InnerWriter.WriteLineAsync();
            if (AutoFlush) await InnerWriter.FlushAsync();
            return;
        }

        // ReSharper disable once MethodHasAsyncOverload
        InnerWriter.WriteLine();
        // ReSharper disable once MethodHasAsyncOverload
        if (AutoFlush) InnerWriter.Flush();
    }

    public override async Task WriteLineAsync(char value)
    {
        if (UseAsync)
        {
            await InnerWriter.WriteLineAsync(value);
            if (AutoFlush) await InnerWriter.FlushAsync();
            return;
        }

        // ReSharper disable once MethodHasAsyncOverload
        InnerWriter.WriteLine(value);
        // ReSharper disable once MethodHasAsyncOverload
        if (AutoFlush) InnerWriter.Flush();
    }

    public override async Task WriteLineAsync(char[] buffer, int index, int count)
    {
        if (UseAsync)
        {
            await InnerWriter.WriteLineAsync(buffer, index, count);
            if (AutoFlush) await InnerWriter.FlushAsync();
            return;
        }

        // ReSharper disable once MethodHasAsyncOverload
        InnerWriter.WriteLine(buffer, index, count);
        // ReSharper disable once MethodHasAsyncOverload
        if (AutoFlush) InnerWriter.Flush();
    }

    public override async Task WriteLineAsync(string? value)
    {
        if (UseAsync)
        {
            await InnerWriter.WriteLineAsync(value);
            if (AutoFlush) await InnerWriter.FlushAsync();
            return;
        }

        // ReSharper disable once MethodHasAsyncOverload
        InnerWriter.WriteLine(value);
        // ReSharper disable once MethodHasAsyncOverload
        if (AutoFlush) InnerWriter.Flush();
    }

    public async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
    {
        await InnerWriter.FlushAsync();

        if (_stream == null)
        {
            var (owner, count) = ByteToChars(buffer.Span);

            try
            {
#if NET
                await InnerWriter.WriteAsync(owner.Memory.Slice(0, count), token);
#else
                await InnerWriter.WriteAsync(owner.Memory.Span.Slice(0, count).ToString());
#endif
            }
            finally
            {
                owner.Dispose();
            }

            return;
        }

        await _stream.WriteAsync(buffer, token);
        if (AutoFlush) await _stream.FlushAsync(token);
    }

    public async Task WriteLineAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
    {
        await InnerWriter.FlushAsync();

        if (_stream == null)
        {
            var (owner, count) = ByteToChars(buffer.Span);

            try
            {
#if NET
                await InnerWriter.WriteLineAsync(owner.Memory.Slice(0, count), token);
#else
                await InnerWriter.WriteLineAsync(owner.Memory.Span.Slice(0, count).ToString());
#endif
            }
            finally
            {
                owner.Dispose();
            }

            return;
        }

        await _stream.WriteAsync(buffer, token);
        await _stream.WriteAsync(_newLineBytes, token);
        if (AutoFlush) await _stream.FlushAsync(token);
    }

    private (IMemoryOwner<char> Owner, int Length) ByteToChars(ReadOnlySpan<byte> buffer)
    {
        var maxCharCount = Encoding.GetMaxCharCount(buffer.Length);
        var owner = MemoryPool<char>.Shared.Rent(maxCharCount);
        var chars = owner.Memory.Span;
        var charCount = Encoding.GetChars(buffer, chars);

        return (owner, charCount);
    }

    public override string ToString()
    {
        return InnerWriter.ToString()!;
    }

#if NET
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (_disposeInnerWriter)
        {
            await InnerWriter.DisposeAsync();
        }
    }
#else
    public ValueTask DisposeAsync()
    {
        if (_disposeInnerWriter)
        {
            InnerWriter.Dispose();
        }

        return default;
    }
#endif
}
