using Microsoft.CodeAnalysis;
using WebFormsCore.Collections.Comparers;
using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public class ControlNode : ElementNode
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
}
