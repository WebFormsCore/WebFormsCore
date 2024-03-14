using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebFormsCore.UI;

namespace WebFormsCore.Events;

public class PageService : IPageService
{
    public virtual ValueTask BeforeInitializeAsync(Page page, CancellationToken token)
    {
        return default;
    }

    public virtual ValueTask AfterInitializeAsync(Page page, CancellationToken token)
    {
        return default;
    }

    public virtual ValueTask BeforeLoadAsync(Page page, CancellationToken token)
    {
        return default;
    }

    public virtual ValueTask AfterLoadAsync(Page page, CancellationToken token)
    {
        return default;
    }

    public virtual ValueTask BeforePreRenderAsync(Page page, CancellationToken token)
    {
        return default;
    }

    public virtual ValueTask AfterPreRenderAsync(Page page, CancellationToken token)
    {
        return default;
    }

    public virtual Task BeforePostbackAsync(Page page, CancellationToken token)
    {
        return Task.CompletedTask;
    }

    public virtual Task AfterPostbackAsync(Page page, CancellationToken token)
    {
        return Task.CompletedTask;
    }

    public virtual ValueTask RenderHeadAsync(Page page, HtmlTextWriter writer, CancellationToken token)
    {
        return default;
    }

    public virtual ValueTask RenderBodyAsync(Page page, HtmlTextWriter writer, CancellationToken token)
    {
        return default;
    }
}
