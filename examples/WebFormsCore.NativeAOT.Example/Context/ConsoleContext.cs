using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore.NativeAOT.Example.Context;

public class ConsoleContext : IHttpContext
{
    public ConsoleContext(AsyncServiceScope scope)
    {
        Scope = scope;
    }

    public AsyncServiceScope Scope { get; set; }

    public ConsoleRequest Request { get; } = new();

    public ConsoleResponse Response { get; } = new();

    public IServiceProvider RequestServices => Scope.ServiceProvider;

    public CancellationToken RequestAborted => default;

    IHttpRequest IHttpContext.Request => Request;

    IHttpResponse IHttpContext.Response => Response;
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
        Body = new MemoryStream();
    }

    public MemoryStream Body { get; }

    public string ContentType { get; set; } = "text/html";

    public IDictionary<string, StringValues> Headers { get; } = new Dictionary<string, StringValues>();

    Stream IHttpResponse.Body => Body;
}
