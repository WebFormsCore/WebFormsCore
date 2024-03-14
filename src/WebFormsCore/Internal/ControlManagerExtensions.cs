using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebFormsCore.UI;

namespace WebFormsCore;

public static class ControlManagerExtensions
{
    public static Task<Page> RenderPageAsync(this IPageManager controlManager, HttpContext context, string path)
    {
        return controlManager.RenderPageAsync(
            context,
            path,
            context.RequestAborted
        );
    }

    public static Task<Page> RenderPageAsync(this IPageManager controlManager, HttpContext context, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        return controlManager.RenderPageAsync(
            context,
            type,
            context.RequestAborted
        );
    }

    public static Task RenderPageAsync(this IPageManager controlManager, HttpContext context, Page page)
    {
        return controlManager.RenderPageAsync(
            context,
            page,
            context.RequestAborted
        );
    }
}
