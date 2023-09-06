using Microsoft.CodeAnalysis;
using WebFormsCore.Collections.Comparers;
using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public interface ITypedNode
{
    INamedTypeSymbol Type { get; }

    List<TemplateNode> Templates { get; }

    List<Node> Children { get; }

    List<PropertyNode> Properties { get; }

    List<EventNode> Events { get; }

    Dictionary<TokenString, TokenString> Attributes { get; }
}

public class ControlNode : ElementNode, ITypedNode
{
    public ControlNode(INamedTypeSymbol controlType)
        : base(NodeType.Control)
    {
        ControlType = controlType;
    }

    public string? Id { get; set; }

    public List<PropertyNode> Properties { get; set; } = new();

    public List<EventNode> Events { get; set; } = new();

    public List<TemplateNode> Templates { get; set; } = new();

    public INamedTypeSymbol ControlType { get; }

    public string? FieldName { get; set; }

    public string DisplayControlType => ControlType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    INamedTypeSymbol ITypedNode.Type => ControlType;
}
