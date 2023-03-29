using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Web;

namespace WebFormsCore.Implementation;

internal class HttpContextImpl : IHttpContext
{
    private HttpContext _httpContext;
    private HttpRequestImpl _request = new();
    private HttpResponseImpl _response = new();
    private readonly FeatureCollection _features = new();

    public void SetHttpContext(HttpContext httpContext, IServiceProvider requestServices)
    {
        _httpContext = httpContext;
        _request.SetHttpRequest(httpContext.Request);
        _response.SetHttpResponse(httpContext.Response);
        RequestServices = requestServices;
    }

    public void Reset()
    {
        _features.Reset();
        _request.Reset();
        _response.Reset();
        _httpContext = null!;
    }

    public IHttpRequest Request => _request;
    public IHttpResponse Response => _response;
    public IServiceProvider RequestServices { get; internal set; }
    public CancellationToken RequestAborted => _httpContext.Request.TimedOutToken;
    public IFeatureCollection Features => _features;
}
