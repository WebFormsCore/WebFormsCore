using System.Text;
using WebForms.Collections;
using WebForms.Models;

namespace WebForms.Nodes;

public class DirectiveNode : Node, IAttributeNode
{
    public DirectiveNode() : base(NodeType.Directive)
    {
    }

    public DirectiveType DirectiveType { get; set; }

    public Dictionary<TokenString, TokenString> Attributes { get; set; } = new(AttributeCompare.IgnoreCase);

    public override void Write(CompileContext builder)
    {
    }
}
