using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HttpStack;
using WebFormsCore.UI;

namespace WebFormsCore;

public interface IPageManager
{
    Task<Page> RenderPageAsync(
        IHttpContext context,
        string path,
        CancellationToken token);

    [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
    Task<Page> RenderPageAsync(
        IHttpContext context,
        Type pageType,
        CancellationToken token);

    Task RenderPageAsync(IHttpContext context,
        Page page,
        CancellationToken token);
}
