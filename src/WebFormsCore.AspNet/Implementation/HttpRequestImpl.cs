using System.Collections.Generic;
using System.IO;
using System.Web;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore.Implementation;

internal class HttpRequestImpl : IHttpRequest
{
    private HttpRequest _httpRequest;
    private StringValuesImpl _form = new();
    private IReadOnlyDictionary<string, StringValues> _formValue;

    public void SetHttpRequest(HttpRequest httpRequest)
    {
        _httpRequest = httpRequest;
        _form.SetNameValueCollection(httpRequest.Form);
        _formValue = null;
    }

    public void Reset()
    {
        _form.Reset();
        _httpRequest = null!;
        _formValue = null;
    }

    public string Method => _httpRequest.HttpMethod;
    public string ContentType => _httpRequest.ContentType;
    public Stream Body => _httpRequest.InputStream;
    public string Path => _httpRequest.Path;

    public IReadOnlyDictionary<string, StringValues> Form
    {
        get => _formValue ?? _form;
        set => _formValue = value;
    }
}
