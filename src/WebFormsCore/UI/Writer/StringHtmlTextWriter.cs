using System;
using System.Text;
using System.Threading.Tasks;

namespace WebFormsCore.UI;

public class StringHtmlTextWriter : HtmlTextWriter
{
    private readonly StringBuilder _stringBuilder = new();

    protected override bool ForceAsync => false;

    protected override void Flush(ReadOnlySpan<char> buffer)
    {
        _stringBuilder.Append(buffer);
    }

    protected override ValueTask FlushAsync(ReadOnlyMemory<char> buffer)
    {
        _stringBuilder.Append(buffer.Span);
        return default;
    }

    public override string ToString()
    {
        return _stringBuilder.ToString();
    }
}