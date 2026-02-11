using Microsoft.CodeAnalysis;
using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public class TemplateNode : ElementNode
{
    public string ClassName { get; set; } = default!;

    public Token Property { get; set; }

    public string? ControlsType { get; set; }

    public List<ContainerNode> RenderMethods { get; set; } = new();

    public bool IsSingleInstance { get; set; }

    public List<ControlId> Ids { get; set; } = new();

    public override string? VariableName
    {
        get => null;
        set
        {
            // ignore.
        }
    }

    public INamedTypeSymbol? ItemType { get; set; }
}
