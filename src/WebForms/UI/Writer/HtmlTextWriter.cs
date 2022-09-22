using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI;

public class HtmlTextWriter : TextWriter
{
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
    private readonly List<KeyValuePair<string, string?>> _attributes = new();
    private readonly List<KeyValuePair<string, string?>> _styleAttributes = new();

    private TextWriter? _innerWriter;
    private readonly Stream _stream;

    internal void Initialize(TextWriter writer)
    {
        _innerWriter = writer;
    }

    internal void Clear()
    {
        _openTags.Clear();
        _attributes.Clear();
        _styleAttributes.Clear();
        _innerWriter = null;
    }

    public HtmlTextWriter(TextWriter writer, Stream stream)
    {
        _stream = stream;
        _innerWriter = writer;
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

    public void AddAttribute(HtmlTextWriterAttribute key, string value) => AddAttribute(key, value, true);
    public void AddAttribute(HtmlTextWriterAttribute key, string value, bool encode) => AddAttribute(key.ToName(), value, encode);

    public void AddAttribute(string name, string? value) => AddAttribute(name, value, true);
    public void AddAttribute(string name, string? value, bool encode)
    {
        if (encode) value = WebUtility.HtmlEncode(value);

        _attributes.Add(new KeyValuePair<string, string?>(name, value));
    }

    public void AddStyleAttribute(HtmlTextWriterStyle key, string? value) => AddStyleAttribute(key, value, true);
    public void AddStyleAttribute(HtmlTextWriterStyle key, string? value, bool encode) => AddStyleAttribute(key.ToName(), value, encode);

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
        await InnerWriter.WriteAsync(TagRightChar);
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

    //
    // Coordination
    //

    void BeforeWrite()
    {

    }

    Task BeforeWriteAsync()
    {
        return Task.CompletedTask;
    }

    void AfterWriteLine()
    {
    }

    //
    // HTML-specific writes
    //

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

    public async ValueTask WriteBreakAsync(CancellationToken cancellationToken = default)
    {
        await WriteAsync(TagLeftChar);
        await WriteAsync("br");
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
        BeforeWrite();
        InnerWriter.Write(value);
    }

    public override void Write(uint value)
    {
        BeforeWrite();
        InnerWriter.Write(value);
    }

    public override void Write(string format, params object?[] arg)
    {
        BeforeWrite();
        InnerWriter.Write(format, arg);
    }

    public override void Write(string format, object? arg0, object? arg1, object? arg2)
    {
        BeforeWrite();
        InnerWriter.Write(format, arg0, arg1, arg2);
    }

    public override void Write(string format, object? arg0, object? arg1)
    {
        BeforeWrite();
        InnerWriter.Write(format, arg0, arg1);
    }

    public override void Write(string format, object? arg0)
    {
        BeforeWrite();
        InnerWriter.Write(format, arg0);
    }

    public override void Write(string? value)
    {
        BeforeWrite();
        InnerWriter.Write(value);
    }

    public override void Write(object? value)
    {
        BeforeWrite();
        InnerWriter.Write(value);
    }

    public override void Write(long value)
    {
        BeforeWrite();
        InnerWriter.Write(value);
    }

    public override void Write(int value)
    {
        BeforeWrite();
        InnerWriter.Write(value);
    }

    public override void Write(double value)
    {
        BeforeWrite();
        InnerWriter.Write(value);
    }

    public override void Write(decimal value)
    {
        BeforeWrite();
        InnerWriter.Write(value);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        BeforeWrite();
        InnerWriter.Write(buffer, index, count);
    }

    public override void Write(char[]? buffer)
    {
        BeforeWrite();
        InnerWriter.Write(buffer);
    }

    public override void Write(char value)
    {
        BeforeWrite();
        InnerWriter.Write(value);
    }

    public override void Write(bool value)
    {
        BeforeWrite();
        InnerWriter.Write(value);
    }

    public override void Write(float value)
    {
        BeforeWrite();
        InnerWriter.Write(value);
    }

    public async Task WriteObjectAsync(object? value)
    {
        if (value == null) return;

        await BeforeWriteAsync();
        await InnerWriter.WriteAsync(value.ToString());
    }

    public override async Task WriteAsync(string? value)
    {
        await BeforeWriteAsync();
        await InnerWriter.WriteAsync(value);
    }

    public override async Task WriteAsync(char value)
    {
        await BeforeWriteAsync();
        await InnerWriter.WriteAsync(value);
    }

#if NET
    public override async Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = new CancellationToken())
    {
        await BeforeWriteAsync();
        await InnerWriter.WriteAsync(value, cancellationToken);
    }

    public override async Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        await BeforeWriteAsync();
        await InnerWriter.WriteAsync(buffer, cancellationToken);
    }

    public override async Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        await BeforeWriteAsync();
        await InnerWriter.WriteAsync(buffer, cancellationToken);
    }

    public override void Write(ReadOnlySpan<char> buffer)
    {
        BeforeWrite();
        base.Write(buffer);
    }

    public override void Write(StringBuilder? value)
    {
        BeforeWrite();
        base.Write(value);
    }

    public override void WriteLine(ReadOnlySpan<char> buffer)
    {
        BeforeWrite();
        base.WriteLine(buffer);
    }

    public override void WriteLine(StringBuilder? value)
    {
        BeforeWrite();
        base.WriteLine(value);
    }

    public override async Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = new CancellationToken())
    {
        await BeforeWriteAsync();
        await base.WriteLineAsync(value, cancellationToken);
    }
#endif

    public override async Task WriteAsync(char[] buffer, int index, int count)
    {
        await BeforeWriteAsync();
        await InnerWriter.WriteAsync(buffer, index, count);
    }

    //
    // WriteLine
    //

    public override void WriteLine(string format, object? arg0)
    {
        BeforeWrite();
        InnerWriter.WriteLine(format, arg0);
        AfterWriteLine();
    }

    public override void WriteLine(ulong value)
    {
        BeforeWrite();
        InnerWriter.WriteLine(value);
        AfterWriteLine();
    }

    public override void WriteLine(uint value)
    {
        BeforeWrite();
        InnerWriter.WriteLine(value);
        AfterWriteLine();
    }

    public override void WriteLine(string format, params object?[] arg)
    {
        BeforeWrite();
        InnerWriter.WriteLine(format, arg);
        AfterWriteLine();
    }

    public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
    {
        BeforeWrite();
        InnerWriter.WriteLine(format, arg0, arg1, arg2);
        AfterWriteLine();
    }

    public override void WriteLine(string format, object? arg0, object? arg1)
    {
        BeforeWrite();
        InnerWriter.WriteLine(format, arg0, arg1);
        AfterWriteLine();
    }

    public override void WriteLine(string? value)
    {
        BeforeWrite();
        InnerWriter.WriteLine(value);
        AfterWriteLine();
    }

    public override void WriteLine(float value)
    {
        BeforeWrite();
        InnerWriter.WriteLine(value);
        AfterWriteLine();
    }

    public override void WriteLine()
    {
        BeforeWrite();
        InnerWriter.WriteLine();
        AfterWriteLine();
    }

    public override void WriteLine(long value)
    {
        BeforeWrite();
        InnerWriter.WriteLine(value);
        AfterWriteLine();
    }

    public override void WriteLine(int value)
    {
        BeforeWrite();
        InnerWriter.WriteLine(value);
        AfterWriteLine();
    }

    public override void WriteLine(double value)
    {
        BeforeWrite();
        InnerWriter.WriteLine(value);
        AfterWriteLine();
    }

    public override void WriteLine(decimal value)
    {
        BeforeWrite();
        InnerWriter.WriteLine(value);
        AfterWriteLine();
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        BeforeWrite();
        InnerWriter.WriteLine(buffer, index, count);
        AfterWriteLine();
    }

    public override void WriteLine(char[]? buffer)
    {
        BeforeWrite();
        InnerWriter.WriteLine(buffer);
        AfterWriteLine();
    }

    public override void WriteLine(char value)
    {
        BeforeWrite();
        InnerWriter.WriteLine(value);
        AfterWriteLine();
    }

    public override void WriteLine(bool value)
    {
        BeforeWrite();
        InnerWriter.WriteLine(value);
        AfterWriteLine();
    }

    public override void WriteLine(object? value)
    {
        BeforeWrite();
        InnerWriter.WriteLine(value);
        AfterWriteLine();
    }

    public override async Task WriteLineAsync()
    {
        await BeforeWriteAsync();
        await InnerWriter.WriteLineAsync();
        AfterWriteLine();
    }

    public override async Task WriteLineAsync(char value)
    {
        await BeforeWriteAsync();
        await InnerWriter.WriteLineAsync(value);
        AfterWriteLine();
    }

    public override async Task WriteLineAsync(char[] buffer, int index, int count)
    {
        await BeforeWriteAsync();
        await InnerWriter.WriteLineAsync(buffer, index, count);
        AfterWriteLine();
    }

    public override async Task WriteLineAsync(string? value)
    {
        await BeforeWriteAsync();
        await InnerWriter.WriteLineAsync(value);
        AfterWriteLine();
    }

    public async Task WriteAsync(Memory<byte> buffer, CancellationToken token)
    {
        await BeforeWriteAsync();
        await InnerWriter.FlushAsync();
#if NET
        await _stream.WriteAsync(buffer, token);
#else
        await _stream.WriteAsync(buffer.ToArray(), 0, buffer.Length, token);
#endif
    }
}
