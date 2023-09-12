using System;
using System.Text;
using System.Threading.Tasks;

namespace WebFormsCore.UI;

public class StringHtmlTextWriter : HtmlTextWriter
{
    private readonly StringBuilder _stringBuilder = new();

    protected override bool ForceAsync => false;

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