using System.Diagnostics;
using WebFormsCore.Nodes;

namespace WebFormsCore.Language;

internal class ParserContainer
{
    private readonly Stack<ElementNode> _elements = new();
    private readonly Stack<TemplateNode> _templates = new();

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

    public ElementNode? Current { get; private set; }

    public TemplateNode? Template { get; private set; }

    public int ControlId { get; set; }

    public int RenderId { get; set; }

    private void Add(Node node)
    {
        if (node is TemplateNode templateNode)
        {
            _templates.Push(templateNode);
            Template = templateNode;
        }
        else
        {
            Parent.Children.Add(node);
        }

        node.Parent = Current;
    }

    public void AddDirective(DirectiveNode node)
    {
        Root.Directives.Add(node);
        Add(node);
    }
    public void Push(ElementNode node)
    {
        Add(node);
        _elements.Push(node);
        Current = node;
        Parent = node;
    }

    public ElementNode? Pop()
    {
        if (_elements.Count == 0)
        {
            return null;
        }
        
        var current = _elements.Pop();
        Current = current.Parent;
        Parent = (ContainerNode?) Current ?? Root;

        if (current is TemplateNode templateNode)
        {
            Debug.Assert(_templates.Count > 0);
            var template = _templates.Pop();
            Debug.Assert(template == templateNode);
            Template = _templates.Count > 0 ? _templates.Peek() : null;
        }

        return current;
    }

    public void AddStatement(StatementNode node)
    {
        if (Parent.RenderName == null)
        {
            Parent.RenderName = $"Render_{RenderId++}";

            if (Template != null)
            {
                Template.RenderMethods.Add(Parent);
            }
            else
            {
                Root.RenderMethods.Add(Parent);
            }
        }

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
