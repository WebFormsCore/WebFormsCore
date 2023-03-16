using System.Collections.Generic;
using System.IO;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore.Implementation;

internal class HttpRequestImpl : IHttpRequest
{
    private readonly Dictionary<string, StringValues> _emptyForm = new();
    private HttpRequest _httpRequest = default!;
    private readonly StringValuesImpl _form = new();

    public void SetHttpRequest(HttpRequest httpRequest)
    {
        _httpRequest = httpRequest;
        Form = null!;

        if (httpRequest.HasFormContentType)
        {
            Form = _form;
            _form.SetFormCollection(httpRequest.Form);
        }
        else
        {
            Form = _emptyForm;
        }
    }

    public void Reset()
    {
        _form.Reset();
        _httpRequest = null!;
        Form = null!;
    }

    public string Method => _httpRequest.Method;
    public string? ContentType => _httpRequest.ContentType;
    public Stream Body => _httpRequest.Body;
    public string Path => _httpRequest.Path;

    public IReadOnlyDictionary<string, StringValues> Form { get; set; } = null!;
}
