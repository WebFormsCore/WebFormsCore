using Microsoft.AspNetCore.Http;

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
        var path = _application.GetPath(context);

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

        return path != null
            ? _application.ProcessAsync(context, path, context.RequestServices, context.RequestAborted)
            : Task.CompletedTask;
    }
}
