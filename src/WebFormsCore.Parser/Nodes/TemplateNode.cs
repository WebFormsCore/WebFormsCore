using Microsoft.CodeAnalysis;
using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public class CollectionNode : ElementNode, ITypedNode
{
    public CollectionNode()
        : base(NodeType.Collection)
    {
    }

    public string Property { get; set; } = null!;
    public INamedTypeSymbol PropertyType { get; set; } = default!;
    public List<TemplateNode> Templates { get; set; } = new();
    public List<PropertyNode> Properties { get; set; } = new();

    public List<EventNode> Events { get; set; } = new();

    INamedTypeSymbol ITypedNode.Type => PropertyType;
}

public class TemplateNode : ElementNode
{
    public string ClassName { get; set; } = default!;

    public Token Property { get; set; }

    public string? ControlsType { get; set; }

    public List<ContainerNode> RenderMethods { get; set; } = new();

    public List<ControlId> Ids { get; set; } = new();

    public override string? VariableName
    {
        get => null;
        set
        {
            // ignore.
        }
    }
}
