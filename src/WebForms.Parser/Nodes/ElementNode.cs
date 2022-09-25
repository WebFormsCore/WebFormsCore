using System.Diagnostics;
using System.Text;
using WebFormsCore.Collections.Comparers;
using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public class ElementNode : ContainerNode, IAttributeNode
{
    protected ElementNode(NodeType type)
        : base(type)
    {
    }

    public ElementNode()
        : base(NodeType.Element)
    {
    }

    public TokenString Name => StartTag.Name;

    public TokenString? Namespace => StartTag.Namespace;

    public HtmlTagNode StartTag { get; set; } = new();

    public HtmlTagNode? EndTag { get; set; }

    public virtual string? VariableName { get; set; }

    public Dictionary<TokenString, TokenString> Attributes { get; set; } = new(AttributeCompare.IgnoreCase);
}
