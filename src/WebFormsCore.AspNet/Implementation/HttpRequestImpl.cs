using System.Collections.Generic;
using System.IO;
using System.Web;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore.Implementation;

internal class HttpRequestImpl : IHttpRequest
{
    private HttpRequest _httpRequest;
    private readonly NameValueDictionary _form = new();
    private readonly NameValueDictionary _query = new();
    private IReadOnlyDictionary<string, StringValues> _formValue;
    private IReadOnlyDictionary<string, StringValues> _queryValue;

    public void SetHttpRequest(HttpRequest httpRequest)
    {
        _httpRequest = httpRequest;
        _form.SetNameValueCollection(httpRequest.Form);
        _query.SetNameValueCollection(httpRequest.QueryString);
        _formValue = null;
        _queryValue = null;
    }

    public void Reset()
    {
        _form.Reset();
        _query.Reset();
        _httpRequest = null!;
        _formValue = null;
        _queryValue = null;
    }

    public string Method => _httpRequest.HttpMethod;
    public string Scheme => _httpRequest.Url.Scheme;
    public bool IsHttps => _httpRequest.IsSecureConnection;
    public string Protocol => _httpRequest.ServerVariables["SERVER_PROTOCOL"];
    public string ContentType => _httpRequest.ContentType;
    public Stream Body => _httpRequest.InputStream;
    public string Path => _httpRequest.Path;

    public IReadOnlyDictionary<string, StringValues> Query
    {
        get => _queryValue ?? _query;
        set => _queryValue = value;
    }

    public IReadOnlyDictionary<string, StringValues> Form
    {
        get => _formValue ?? _form;
        set => _formValue = value;
    }
}
