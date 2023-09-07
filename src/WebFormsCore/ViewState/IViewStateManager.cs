using System.Buffers;
using System.Threading.Tasks;
using HttpStack;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore;

public interface IViewStateManager
{
    bool EnableViewState { get; }

    ValueTask<IMemoryOwner<byte>> WriteAsync(Control control, out int length);

    ValueTask LoadAsync(Control control, string viewState);
}
