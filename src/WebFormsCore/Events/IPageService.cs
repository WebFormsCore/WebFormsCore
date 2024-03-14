using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebFormsCore.UI;

namespace WebFormsCore.Events;

public interface IPageService
{
    ValueTask BeforeInitializeAsync(Page page, CancellationToken token);

    ValueTask AfterInitializeAsync(Page page, CancellationToken token);

    ValueTask BeforeLoadAsync(Page page, CancellationToken token);

    ValueTask AfterLoadAsync(Page page, CancellationToken token);

    ValueTask BeforePreRenderAsync(Page page, CancellationToken token);

    ValueTask AfterPreRenderAsync(Page page, CancellationToken token);

    Task BeforePostbackAsync(Page page, CancellationToken token);

    Task AfterPostbackAsync(Page page, CancellationToken token);

    ValueTask RenderHeadAsync(Page page, HtmlTextWriter writer, CancellationToken token);

    ValueTask RenderBodyAsync(Page page, HtmlTextWriter writer, CancellationToken token);
}