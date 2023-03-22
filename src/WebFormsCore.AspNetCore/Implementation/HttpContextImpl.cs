using System;
using System.Threading;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace WebFormsCore.Implementation;

internal class HttpContextImpl : IHttpContext
{
    private HttpContext _httpContext = null!;
    private HttpRequestImpl _request = new();
    private HttpResponseImpl _response = new();
    private FeatureCollectionImpl _features = new();

    public void SetHttpContext(HttpContext httpContext)
    {
        _httpContext = httpContext;
        _request.SetHttpRequest(httpContext.Request);
        _response.SetHttpResponse(httpContext.Response);
        _features.SetFeatureCollection(httpContext.Features);
    }

    public void Reset()
    {
        _request.Reset();
        _response.Reset();
        _httpContext = null!;
    }

    public IHttpRequest Request => _request;
    public IHttpResponse Response => _response;
    public IServiceProvider RequestServices => _httpContext.RequestServices;
    public CancellationToken RequestAborted => _httpContext.RequestAborted;
    public IFeatureCollection Features => _features;
}
