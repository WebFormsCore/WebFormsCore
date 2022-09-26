using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace WebFormsCore.SourceGenerator.Tests.Utils;

public class MemoryAdditionalText : AdditionalText
{
    public MemoryAdditionalText(string path, string text)
    {
        Path = path;
        Text = text;
    }

    public override string Path { get; }

    public string Text { get; }

    public override SourceText GetText(CancellationToken cancellationToken = new CancellationToken())
    {
        return SourceText.From(Text);
    }
}
