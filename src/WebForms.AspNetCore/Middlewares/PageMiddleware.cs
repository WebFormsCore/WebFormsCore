using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore.Middlewares;

public class PageMiddleware
{
    private readonly RequestDelegate _next;

    public PageMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var services = context.RequestServices.GetRequiredService<IWebFormsApplication>();
        var handled = await services.ProcessAsync(context, context.RequestServices, context.RequestAborted);

        if (!handled)
        {
            await _next(context);
        }
    }

}
