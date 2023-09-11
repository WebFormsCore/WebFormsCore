using WebFormsCore.Collections.Comparers;
using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public class DirectiveNode : Node, IAttributeNode
{
    public DirectiveNode() : base(NodeType.Directive)
    {
    }

    public DirectiveType DirectiveType { get; set; }

    public Dictionary<TokenString, AttributeValue> Attributes { get; set; } = new(AttributeCompare.IgnoreCase);

    public List<PropertyNode> Properties { get; set; } = new();
}
