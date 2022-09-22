using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public class HtmlTagNode
{
    public TokenString? Namespace { get; set; }

    public TokenString Name { get; set; }

    public TokenRange Range { get; set; }

    public TokenRange ElementRange => Namespace.HasValue ? Namespace.Value.Range.WithEnd(Name.Range.End) : Name.Range;
}
