using System.Threading.Tasks;
using WebFormsCore.UI;

namespace WebFormsCore.Events;

public interface IPageService
{
    ValueTask BeforeInitializeAsync(Page page);

    ValueTask AfterInitializeAsync(Page page);

    ValueTask BeforeRenderAsync(Page page);

    ValueTask AfterRenderAsync(Page page);
}
