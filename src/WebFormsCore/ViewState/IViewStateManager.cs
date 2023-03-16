using System.Buffers;
using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore;

public interface IViewStateManager
{
    bool EnableViewState { get; }

    IMemoryOwner<byte> Write(Control control, out int length);

    ValueTask<HtmlForm?> LoadAsync(IHttpContext context, Page page);
}
