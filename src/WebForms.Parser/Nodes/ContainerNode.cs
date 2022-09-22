namespace WebFormsCore.Nodes;

public abstract class ContainerNode : Node
{
    protected ContainerNode(NodeType type) : base(type)
    {
    }

    public List<Node> Children { get; set; } = new();

    public override void WriteClass(CompileContext context)
    {
        foreach (var child in Children)
        {
            child.WriteClass(context);
        }
    }

    public override void Write(CompileContext builder)
    {
        foreach (var child in Children)
        {
            child.Write(builder);
        }
    }
}