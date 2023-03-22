using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore.Implementation;

internal class HttpRequestImpl : IHttpRequest
{
    private readonly Dictionary<string, StringValues> _form = new();
    private IDictionary<string, object> _env;
    private IReadOnlyDictionary<string, StringValues> _formValue;

    public void SetHttpRequest(IDictionary<string, object> env)
    {
        _env = env;
        _formValue = null;
    }

    public void Reset()
    {
        _form.Clear();
        _env = null!;
        _formValue = null;
    }

    public string Method => _env["owin.RequestMethod"] as string;
    public string Scheme => _env["owin.RequestScheme"] as string;
    public bool IsHttps => Scheme == "https";
    public string Protocol => _env["owin.RequestProtocol"] as string;
    public string ContentType => _env["owin.RequestHeaders"] is IDictionary<string, string[]> headers && headers.TryGetValue("Content-Type", out var values) ? values[0] : null;
    public Stream Body => _env["owin.RequestBody"] as Stream;
    public string Path => _env["owin.RequestPath"] as string;

    public IReadOnlyDictionary<string, StringValues> Form
    {
        get => _formValue ?? _form;
        set => _formValue = value;
    }
}
