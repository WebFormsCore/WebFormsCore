using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public abstract class Node
{
    protected Node(NodeType type)
    {
        Type = type;
    }

    public NodeType Type { get; }
    
    public TokenRange Range { get; set; }
    
    public ElementNode? Parent { get; set; }

    public string? ParentVariableName => Parent?.VariableName;

    public virtual void WriteClass(CompileContext context)
    {
    }
}