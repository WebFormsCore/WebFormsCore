using WebForms.Nodes;

namespace WebForms;

internal class ParserContainer
{
    private readonly Stack<HtmlNode> _stack = new();

    public ParserContainer()
    {
        Root = new RootNode();
        Parent = Root;
    }

    public ParserContainer(ParserContainer parent)
    {
        Root = parent.Root;
        Parent = parent.Parent;
        Current = parent.Current;
    }

    public RootNode Root { get; }

    public ContainerNode Parent { get; private set; }

    public HtmlNode? Current { get; private set; }

    private void Add(Node node)
    {
        Root.AllNodes.Add(node);
        Parent.Children.Add(node);
        node.Parent = Parent;
    }

    public void AddDirective(DirectiveNode node)
    {
        Root.AllDirectives.Add(node);
        Add(node);
    }

    public void Push(HtmlNode node)
    {
        Root.AllHtmlNodes.Add(node);
        Add(node);
        
        _stack.Push(node);
        Current = node;
        Parent = node;
    }

    public HtmlNode? Pop()
    {
        if (_stack.Count == 0)
        {
            return null;
        }
        
        var current = _stack.Pop();
        Current = _stack.Count > 0 ? _stack.Peek() : null;
        Parent = (ContainerNode?) Current ?? Root;
        return current;
    }

    public void AddStatement(StatementNode node)
    {
        Add(node);
    }

    public void AddExpression(ExpressionNode node)
    {
        Add(node);
    }

    public void AddText(TextNode node)
    {
        Add(node);
    }
}
