using System.Collections.Generic;
using System.IO;
using System.Web;
using Microsoft.Extensions.Primitives;

namespace WebFormsCore.Implementation;

public class HttpResponseImpl : IHttpResponse
{
    private HttpResponse _httpResponse;
    private readonly StringValuesImpl _headers = new();

    public void SetHttpResponse(HttpResponse httpResponse)
    {
        _httpResponse = httpResponse;
        _headers.SetNameValueCollection(httpResponse.Headers);
    }

    public void Reset()
    {
        _headers.Reset();
        _httpResponse = null!;
    }

    public Stream Body => _httpResponse.OutputStream;

    public string ContentType
    {
        get => _httpResponse.ContentType;
        set => _httpResponse.ContentType = value;
    }

    public IDictionary<string, StringValues> Headers => _headers;
}
