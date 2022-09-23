using System.Text;
using Microsoft.CodeAnalysis;
using WebFormsCore.Designer;
using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public record HitRange(TokenRange Range, int Type = 0, TokenString? Value = null)
{
    public Node Node { get; set; } = null!;
}

public abstract class Node
{
    protected Node(NodeType type)
    {
        Type = type;
    }

    public NodeType Type { get; }
    
    public TokenRange Range { get; set; }
    
    public ContainerNode? Parent { get; set; }

    public virtual void WriteClass(CompileContext context)
    {
    }

    public virtual void WriteRaw(CompileContext context)
    {
        Write(context);
    }

    public abstract void Write(CompileContext builder);
}

public class CompileContext
{
    public CompileContext(StringBuilder builder)
    {
        Builder = builder;
    }

    public int ControlId { get; set; }

    public StringBuilder Builder { get; }

    public string ParentNode { get; set; } = "this";

    public INamedTypeSymbol? Type { get; set; }

    public bool GenerateFields { get; set; }

    public List<DesignerType>? AssemblyTypes { get; set; }

    public string GetNext()
    {
        return $"@ctrl{ControlId++}";
    }
}
