using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public class TextNode : Node
{
    public TextNode() : base(NodeType.Text)
    {
    }

    public TokenString Text { get; set; }
}
