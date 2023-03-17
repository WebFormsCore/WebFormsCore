using Microsoft.Extensions.Primitives;

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

public class ConsoleRequest : IHttpRequest
{
    public string Method => "GET";

    public string? ContentType => null;

    public Stream Body => Stream.Null;

    public string Path => "/";

    public IReadOnlyDictionary<string, StringValues> Form { get; set; } = new Dictionary<string, StringValues>();
}

public class ConsoleResponse : IHttpResponse
{
    public ConsoleResponse()
    {
        Body = Console.OpenStandardOutput();
    }

    public Stream Body { get; }

    public string ContentType { get; set; } = "text/html";

    public IDictionary<string, StringValues> Headers { get; } = new Dictionary<string, StringValues>();
}
