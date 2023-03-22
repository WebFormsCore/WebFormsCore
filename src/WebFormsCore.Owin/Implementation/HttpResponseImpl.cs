using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore.Implementation;

public class HttpResponseImpl : IHttpResponse
{
    private IDictionary<string, object> _env;
    private readonly HeaderDictionaryImpl _headers = new();

    public void SetHttpResponse(IDictionary<string, object> env)
    {
        _env = env;
        _headers.SetNameValueCollection(env["owin.ResponseHeaders"] as IDictionary<string, string[]>);
    }

    public void Reset()
    {
        _headers.Reset();
        _env = null!;
    }

    public Stream Body => _env["owin.ResponseBody"] as Stream;

    public string ContentType
    {
        get => Headers["Content-Type"];
        set => Headers["Content-Type"] = value;
    }

    public IDictionary<string, StringValues> Headers => _headers;
}
