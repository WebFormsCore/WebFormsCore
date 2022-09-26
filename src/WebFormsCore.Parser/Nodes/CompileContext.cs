using System.Text;
using Microsoft.CodeAnalysis;

namespace WebFormsCore.Nodes;

public class CompileContext
{
    public CompileContext(StringBuilder builder)
    {
        Builder = builder;
    }

    public int TemplateId { get; set; }

    public int ControlId { get; set; }

    public StringBuilder Builder { get; }

    public INamedTypeSymbol? Type { get; set; }

    public bool GenerateFields { get; set; }
}