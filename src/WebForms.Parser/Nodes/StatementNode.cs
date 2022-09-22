using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public class StatementNode : Node
{
    public StatementNode() : base(NodeType.Statement)
    {
    }
    
    public TokenString Text { get; set; }
    
    public override void Write(CompileContext builder)
    {
        builder.Builder.AppendLine(Text.Value);
    }
}
