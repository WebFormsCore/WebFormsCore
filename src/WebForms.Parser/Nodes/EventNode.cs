using Microsoft.CodeAnalysis;

namespace WebFormsCore.Nodes;

public class EventNode : Node
{
    private IMethodSymbol? _invoke;

    public EventNode(IEventSymbol @event, IMethodSymbol method)
        : base(NodeType.Event)
    {
        Event = @event;
        Method = method;
    }

    public IEventSymbol Event { get; init; }

    public IMethodSymbol Method { get; init; }

    public IMethodSymbol? Invoke => _invoke ??= Event.Type?.GetDeep<IMethodSymbol>("Invoke");

    public string EventName => Event.Name;

    public string MethodName => Method.Name;

    public string Arguments => string.Join(", ", Method.Parameters.Select(i => i.Name));

    public bool IsReturnTypeSame => Invoke == null || Method.ReturnType.Equals(Invoke.ReturnType, SymbolEqualityComparer.Default);

    public string? DisplayReturnType => Invoke?.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public string? ReturnValue => Invoke?.ReturnType.Name == "Task" ? "System.Threading.Tasks.Task.CompletedTask" : null;
}