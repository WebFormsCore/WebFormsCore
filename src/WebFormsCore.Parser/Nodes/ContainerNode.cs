namespace WebFormsCore.Nodes;

public abstract class ContainerNode : Node
{
    protected ContainerNode(NodeType type) : base(type)
    {
    }

    public List<Node> Children { get; set; } = new();

    public string? RenderName { get; set; }

    public IEnumerable<Node> AllChildren
    {
        get
        {
            foreach (var child in Children)
            {
                yield return child;

                if (child is not ContainerNode container) continue;

                foreach (var grandChild in container.AllChildren)
                {
                    yield return grandChild;
                }
            }
        }
    }

    public override void WriteClass(CompileContext context)
    {
        foreach (var child in Children)
        {
            child.WriteClass(context);
        }
    }
}
