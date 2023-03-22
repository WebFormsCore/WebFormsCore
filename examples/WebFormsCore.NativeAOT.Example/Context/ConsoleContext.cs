namespace WebFormsCore.NativeAOT.Example.Context;

public class ConsoleContext : IHttpContext
{
    public ConsoleContext(IServiceProvider requestServices)
    {
        RequestServices = requestServices;
    }

    public IHttpRequest Request { get; } = new ConsoleRequest();

    public IHttpResponse Response { get; } = new ConsoleResponse();

    public IServiceProvider RequestServices { get; }
    public CancellationToken RequestAborted => default;
}
