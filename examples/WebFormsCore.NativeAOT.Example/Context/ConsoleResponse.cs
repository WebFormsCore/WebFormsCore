using Microsoft.Extensions.Primitives;

namespace WebFormsCore.NativeAOT.Example.Context;

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