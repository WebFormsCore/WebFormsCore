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
    }

    public void RenderEndTag()
    {
        if (_openTags == null || !_openTags.Any())
            throw new InvalidOperationException();

        var tag = _openTags.Pop();
        Write(EndTagLeftChars);
        Write(tag);
        WriteLine(TagRightChar);
    }

    public async ValueTask RenderEndTagAsync()
    {
        if (_openTags == null || !_openTags.Any())
            throw new InvalidOperationException();

        var tag = _openTags.Pop();
        await WriteAsync(EndTagLeftChars);
        await WriteAsync(tag);
        await WriteLineAsync(TagRightChar);
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

    public void WriteEncodedText(string text) => Write(WebUtility.HtmlEncode(text));

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

    public void WriteEncodedUrlParameter(string urlText) => Write(Uri.EscapeDataString(urlText));

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
    }

    public override void Write(uint value)
    {
        InnerWriter.Write(value);
    }

    public override void Write(string format, params object?[] arg)
    {
        InnerWriter.Write(format, arg);
    }

    public override void Write(string format, object? arg0, object? arg1, object? arg2)
    {
        InnerWriter.Write(format, arg0, arg1, arg2);
    }

    public override void Write(string format, object? arg0, object? arg1)
    {
        InnerWriter.Write(format, arg0, arg1);
    }

    public override void Write(string format, object? arg0)
    {
        InnerWriter.Write(format, arg0);
    }

    public override void Write(string? value)
    {
        InnerWriter.Write(value);
    }

    public override void Write(object? value)
    {
        InnerWriter.Write(value);
    }

    public override void Write(long value)
    {
        InnerWriter.Write(value);
    }

    public override void Write(int value)
    {
        InnerWriter.Write(value);
    }

    public override void Write(double value)
    {
        InnerWriter.Write(value);
    }

    public override void Write(decimal value)
    {
        InnerWriter.Write(value);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        InnerWriter.Write(buffer, index, count);
    }

    public override void Write(char[]? buffer)
    {
        InnerWriter.Write(buffer);
    }

    public override void Write(char value)
    {
        InnerWriter.Write(value);
    }

    public override void Write(bool value)
    {
        InnerWriter.Write(value);
    }

    public override void Write(float value)
    {
        InnerWriter.Write(value);
    }

    public async Task WriteObjectAsync(object? value)
    {
        if (value == null) return;

        await WriteAsync(value.ToString());
    }

    public override Task WriteAsync(string? value)
    {
        if (UseAsync)
        {
            return InnerWriter.WriteAsync(value);
        }

        // ReSharper disable once MethodHasAsyncOverload
        InnerWriter.Write(value);
        return Task.CompletedTask;
    }

    public override Task WriteAsync(char value)
    {
        if (UseAsync)
        {
            return InnerWriter.WriteAsync(value);
        }

        // ReSharper disable once MethodHasAsyncOverload
        InnerWriter.Write(value);
        return Task.CompletedTask;
    }

#if NET
    public override Task WriteAsync(StringBuilder? value,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return InnerWriter.WriteAsync(value, cancellationToken);
    }

    public override Task WriteAsync(ReadOnlyMemory<char> buffer,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return InnerWriter.WriteAsync(buffer, cancellationToken);
    }

    public override Task WriteLineAsync(ReadOnlyMemory<char> buffer,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return InnerWriter.WriteAsync(buffer, cancellationToken);
    }

    public override void Write(ReadOnlySpan<char> buffer)
    {
        InnerWriter.Write(buffer);
    }

    public override void Write(StringBuilder? value)
    {
        InnerWriter.Write(value);
    }

    public override void WriteLine(ReadOnlySpan<char> buffer)
    {
        InnerWriter.WriteLine(buffer);
    }

    public override void WriteLine(StringBuilder? value)
    {
        InnerWriter.WriteLine(value);
    }

    public override Task WriteLineAsync(StringBuilder? value,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return InnerWriter.WriteLineAsync(value, cancellationToken);
    }
#endif

    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        if (UseAsync)
        {
            return InnerWriter.WriteAsync(buffer, index, count);
        }

        // ReSharper disable once MethodHasAsyncOverload
        InnerWriter.Write(buffer, index, count);
        return Task.CompletedTask;
    }

    //
    // WriteLine
    //

    public override void WriteLine(string format, object? arg0)
    {
        InnerWriter.WriteLine(format, arg0);
    }

    public override void WriteLine(ulong value)
    {
        InnerWriter.WriteLine(value);
    }

    public override void WriteLine(uint value)
    {
        InnerWriter.WriteLine(value);
    }

    public override void WriteLine(string format, params object?[] arg)
    {
        InnerWriter.WriteLine(format, arg);
    }

    public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
    {
        InnerWriter.WriteLine(format, arg0, arg1, arg2);
    }

    public override void WriteLine(string format, object? arg0, object? arg1)
    {
        InnerWriter.WriteLine(format, arg0, arg1);
    }

    public override void WriteLine(string? value)
    {
        InnerWriter.WriteLine(value);
    }

    public override void WriteLine(float value)
    {
        InnerWriter.WriteLine(value);
    }

    public override void WriteLine()
    {
        InnerWriter.WriteLine();
    }

    public override void WriteLine(long value)
    {
        InnerWriter.WriteLine(value);
    }

    public override void WriteLine(int value)
    {
        InnerWriter.WriteLine(value);
    }

    public override void WriteLine(double value)
    {
        InnerWriter.WriteLine(value);
    }

    public override void WriteLine(decimal value)
    {
        InnerWriter.WriteLine(value);
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        InnerWriter.WriteLine(buffer, index, count);
    }

    public override void WriteLine(char[]? buffer)
    {
        InnerWriter.WriteLine(buffer);
    }

    public override void WriteLine(char value)
    {
        InnerWriter.WriteLine(value);
    }

    public override void WriteLine(bool value)
    {
        InnerWriter.WriteLine(value);
    }

    public override void WriteLine(object? value)
    {
        InnerWriter.WriteLine(value);
    }

    public override Task WriteLineAsync()
    {
        if (UseAsync)
        {
            return InnerWriter.WriteLineAsync();
        }

        // ReSharper disable once MethodHasAsyncOverload
        InnerWriter.WriteLine();
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync(char value)
    {
        if (UseAsync)
        {
            return InnerWriter.WriteLineAsync(value);
        }

        // ReSharper disable once MethodHasAsyncOverload
        InnerWriter.WriteLine(value);
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync(char[] buffer, int index, int count)
    {
        if (UseAsync)
        {
            return InnerWriter.WriteLineAsync(buffer, index, count);
        }

        // ReSharper disable once MethodHasAsyncOverload
        InnerWriter.WriteLine(buffer, index, count);
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync(string? value)
    {
        if (UseAsync)
        {
            return InnerWriter.WriteLineAsync(value);
        }

        // ReSharper disable once MethodHasAsyncOverload
        InnerWriter.WriteLine(value);
        return Task.CompletedTask;
    }

    public async Task WriteAsync(Memory<byte> buffer, CancellationToken token)
    {
        await InnerWriter.FlushAsync();

        if (_stream == null)
        {
            var (owner, count) = ByteToChars(buffer);

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
    }

    public async Task WriteLineAsync(Memory<byte> buffer, CancellationToken token)
    {
        await InnerWriter.FlushAsync();

        if (_stream == null)
        {
            var (owner, count) = ByteToChars(buffer);

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
    }

    private (IMemoryOwner<char> Owner, int Length) ByteToChars(Memory<byte> buffer)
    {
        var maxCharCount = Encoding.GetMaxCharCount(buffer.Length);
        var owner = MemoryPool<char>.Shared.Rent(maxCharCount);
        var chars = owner.Memory.Span;
        var charCount = Encoding.GetChars(buffer.Span, chars);

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
