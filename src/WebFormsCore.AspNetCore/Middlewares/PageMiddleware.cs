using Microsoft.AspNetCore.Http;
using WebFormsCore.Implementation;

namespace WebFormsCore.Middlewares;

public class PageMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebFormsApplication _application;
    private readonly RequestDelegate _pageHandler;

    public PageMiddleware(RequestDelegate next, IWebFormsApplication application)
    {
        _next = next;
        _application = application;
        _pageHandler = HandleRequest;
    }

    public Task Invoke(HttpContext context)
    {
        var path = _application.GetPath(context.Request.Path);

        if (path != null)
        {
            context.SetEndpoint(new Endpoint(
                _pageHandler,
                new EndpointMetadataCollection(
                    new PageAttribute { Path = path }
                ),
                path
            ));
        }

        return _next(context);
    }

    private Task HandleRequest(HttpContext context)
    {
        var path = context.GetEndpoint()?.Metadata.GetMetadata<PageAttribute>()?.Path;

        if (path == null)
        {
            return Task.CompletedTask;
        }


        var contextImpl = new HttpContextImpl(); // TODO: Pooling
        contextImpl.SetHttpContext(context);

        return _application.ProcessAsync(contextImpl, path, context.RequestAborted);
    }
}
