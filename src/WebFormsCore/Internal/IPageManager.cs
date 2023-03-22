using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;

namespace WebFormsCore;

public interface IPageManager
{
    Task<Page> RenderPageAsync(
        IHttpContext context,
        string path,
        CancellationToken token);

    Task<Page> RenderPageAsync(
        IHttpContext context,
        Type pageType,
        CancellationToken token);

    Task RenderPageAsync(IHttpContext context,
        Page page,
        CancellationToken token);
}
