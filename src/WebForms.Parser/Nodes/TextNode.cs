using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public class TextNode : Node
{
    public TextNode() : base(NodeType.Text)
    {
    }

    public TokenString Text { get; set; }

    public override void Write(CompileContext context)
    {
        var builder = context.Builder;

        builder.Append(context.ParentNode);
        builder.Append(".AddParsedSubObject(WebActivator.CreateLiteral(");
        builder.Append(Text.CodeString);
        builder.AppendLine("));");
    }
}
