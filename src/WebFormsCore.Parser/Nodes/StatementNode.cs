using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public class StatementNode : Node
{
    public StatementNode() : base(NodeType.Statement)
    {
    }
    
    public TokenString Text { get; set; }
}
