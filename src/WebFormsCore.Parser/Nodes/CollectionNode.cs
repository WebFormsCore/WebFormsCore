using Microsoft.CodeAnalysis;

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

    public bool ParseChildren => true;

    INamedTypeSymbol ITypedNode.Type => PropertyType;
}
