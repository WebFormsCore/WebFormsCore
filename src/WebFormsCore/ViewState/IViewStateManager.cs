using System.Buffers;
using System.Threading.Tasks;
using HttpStack;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore;

public interface IViewStateManager
{
    bool EnableViewState { get; }

    IMemoryOwner<byte> WriteBase64(Control control, out int length);

    ValueTask<HtmlForm?> LoadFromRequestAsync(IHttpContext context, Page page);

    ValueTask LoadFromBase64Async(Control control, string viewState);

    ValueTask LoadFromArrayAsync(Control control, byte[] viewState);
}
