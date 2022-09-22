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

    public string? ItemType { get; set; }

    public override void Write(CompileContext context)
    {
        var builder = context.Builder;

        builder.Append(context.ParentNode);
        builder.Append(".AddParsedSubObject(WebActivator.CreateLiteral(");
        builder.Append(Text.Value);
        builder.AppendLine("));");
    }
}
