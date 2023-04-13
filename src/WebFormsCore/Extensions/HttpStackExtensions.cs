using HttpStack;
using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore;

public static class HttpStackExtensions
{
    public static IHttpStackBuilder UseWebFormsCore(this IHttpStackBuilder builder)
    {
        builder.Use((context, next) =>
        {
            var application = context.RequestServices.GetRequiredService<IWebFormsApplication>();

            var path = application.GetPath(context.Request.Path);

            if (path == null)
            {
                context.Response.StatusCode = 404;
                return next();
            }

            return application.ProcessAsync(context, path, context.RequestAborted);
        });

        return builder;
    }
}
