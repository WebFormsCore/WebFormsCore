using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebFormsCore.UI;

namespace WebFormsCore;

public interface IPageManager
{
    Task<Page> RenderPageAsync(
        HttpContext context,
        string path,
        CancellationToken token = default);

    Task<Page> RenderPageAsync(
        HttpContext context,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type pageType,
        CancellationToken token = default);

    Task RenderPageAsync(HttpContext context,
        Page page,
        CancellationToken token = default);

    Task TriggerPostBackAsync(
        Page page,
        string? target,
        string? argument,
        CancellationToken token = default);
}
