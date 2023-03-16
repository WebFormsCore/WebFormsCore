using System.Collections.Generic;
using System.IO;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore.Implementation;

public class HttpResponseImpl : IHttpResponse
{
    private HttpResponse _httpResponse = default!;

    public void SetHttpResponse(HttpResponse httpResponse)
    {
        _httpResponse = httpResponse;
    }

    public void Reset()
    {
        _httpResponse = null!;
    }

    public Stream Body => _httpResponse.Body;

    public string ContentType
    {
        get => _httpResponse.ContentType;
        set => _httpResponse.ContentType = value;
    }

    public IDictionary<string, StringValues> Headers => _httpResponse.Headers;
}
