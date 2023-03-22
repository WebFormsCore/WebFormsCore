using System;
using System.Threading;

namespace WebFormsCore;

public interface IHttpContext
{
    IHttpRequest Request { get; }

    IHttpResponse Response { get; }

    IServiceProvider RequestServices { get; }

    CancellationToken RequestAborted { get; }

    IFeatureCollection Features { get; }
}
