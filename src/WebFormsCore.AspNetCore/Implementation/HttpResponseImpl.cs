using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using WebFormsCore.UI;

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

    public string? ContentType
    {
        get => _httpResponse.ContentType;
#pragma warning disable CS8601 // In ASP.NET Core 7.0 this is nullable
        set => _httpResponse.ContentType = value;
#pragma warning restore CS8601
    }

    public IDictionary<string, StringValues> Headers => _httpResponse.Headers;
}
