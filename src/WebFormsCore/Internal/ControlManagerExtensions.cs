#if NET
using System;
using System.Threading.Tasks;
using WebFormsCore.UI;

namespace WebFormsCore;

public static class ControlManagerExtensions
{
    public static Task<Page> RenderPageAsync(this IControlManager controlManager, HttpContext context, string path)
    {
        return controlManager.RenderPageAsync(
            context,
            context.RequestServices,
            path,
            context.Response.GetOutputStream(),
            context.RequestAborted
        );
    }

    public static Task<Page> RenderPageAsync(this IControlManager controlManager, HttpContext context, Type type)
    {
        return controlManager.RenderPageAsync(
            context,
            context.RequestServices,
            type,
            context.Response.GetOutputStream(),
            context.RequestAborted
        );
    }

    public static Task RenderPageAsync(this IControlManager controlManager, HttpContext context, Page page)
    {
        return controlManager.RenderPageAsync(
            context,
            context.RequestServices,
            page,
            context.Response.GetOutputStream(),
            context.RequestAborted
        );
    }
}
#endif
