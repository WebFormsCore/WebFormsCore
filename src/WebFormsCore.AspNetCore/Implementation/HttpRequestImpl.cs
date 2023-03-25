using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore.Implementation;

internal class HttpRequestImpl : IHttpRequest
{
    private HttpRequest _httpRequest = default!;
    private readonly FormCollectionDictionary _form = new();
    private readonly QueryCollectionDictionary _query = new();

    public void SetHttpRequest(HttpRequest httpRequest)
    {
        _httpRequest = httpRequest;

        if (httpRequest.HasFormContentType)
        {
            Form = _form;
            _form.SetFormCollection(httpRequest.Form);
        }
        else
        {
            Form = EmptyDictionary<string, StringValues>.Instance;
        }

        _query.SetQueryCollection(httpRequest.Query);
        Query = _query;
    }

    public void Reset()
    {
        _form.Reset();
        _query.Reset();
        _httpRequest = null!;
        Form = EmptyDictionary<string, StringValues>.Instance;
        Query = EmptyDictionary<string, StringValues>.Instance;
    }

    public string Method => _httpRequest.Method;
    public string Scheme => _httpRequest.Scheme;
    public bool IsHttps => _httpRequest.IsHttps;
    public string Protocol => _httpRequest.Protocol;
    public string? ContentType => _httpRequest.ContentType;
    public Stream Body => _httpRequest.Body;
    public string Path => _httpRequest.Path;
    public IReadOnlyDictionary<string, StringValues> Query { get; set; } = EmptyDictionary<string, StringValues>.Instance;
    public IReadOnlyDictionary<string, StringValues> Form { get; set; } = EmptyDictionary<string, StringValues>.Instance;
}
