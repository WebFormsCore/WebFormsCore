using Microsoft.Extensions.Primitives;

namespace WebFormsCore.NativeAOT.Example.Context;

public class ConsoleRequest : IHttpRequest
{
    public string Method => "GET";

    public string? ContentType => null;

    public Stream Body => Stream.Null;

    public string Path => "/";

    public IReadOnlyDictionary<string, StringValues> Form { get; set; } = new Dictionary<string, StringValues>();
}