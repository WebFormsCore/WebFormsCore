using System;
using System.Collections.Generic;
using System.Threading;

namespace WebFormsCore.Implementation;

internal class HttpContextImpl : IHttpContext
{
    private IDictionary<string, object> _env;
    private readonly HttpRequestImpl _request = new();
    private readonly HttpResponseImpl _response = new();
    private readonly FeatureCollection _features = new();

    public void SetHttpContext(IDictionary<string, object> env, IServiceProvider requestServices)
    {
        _env = env;
        _request.SetHttpRequest(env);
        _response.SetHttpResponse(env);
        RequestServices = requestServices;
    }

    public void Reset()
    {
        _features.Reset();
        _request.Reset();
        _response.Reset();
        _env = null!;
    }

    public IHttpRequest Request => _request;
    public IHttpResponse Response => _response;
    public IServiceProvider RequestServices { get; private set; }
    public CancellationToken RequestAborted => _env["owin.CallCancelled"] as CancellationToken? ?? CancellationToken.None;
    public IFeatureCollection Features => _features;
}
