using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public class ExpressionNode : Node
{
    public ExpressionNode()
        : base(NodeType.Expression)
    {
    }

    public TokenString Text { get; set; }

    public bool IsEval { get; set; }

    public bool IsEncode { get; set; }

    public string? ItemType { get; set; }
}
