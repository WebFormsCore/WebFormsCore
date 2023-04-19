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

    public IMethodSymbol? Invoke => _invoke ??= Event.Type.GetDeep<IMethodSymbol>("Invoke");

    public string EventName => Event.Name;

    public string MethodName => Method.Name;

    public string Arguments => string.Join(", ", Method.Parameters.Select(i => i.Name));

    public bool IsReturnTypeSame => Invoke == null || Method.ReturnType.Equals(Invoke.ReturnType, SymbolEqualityComparer.Default);

    public bool IsVoid => Method.ReturnsVoid;

    public string? DisplayReturnType => Invoke?.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public string? ReturnValue
    {
        get
        {
            var returnType = Method.ReturnType;

            if (returnType.SpecialType == SpecialType.System_Void)
            {
                return "System.Threading.Tasks.Task.CompletedTask";
            }

            return Method.ReturnType.Name switch
            {
                "Task" => "result",
                "ValueTask" => "result.AsTask()",
                _ => null
            };
        }
    }
}
