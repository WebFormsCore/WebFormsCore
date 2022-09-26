using System.Buffers;
using System.Collections;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public interface IViewStateManager
{
    IMemoryOwner<byte> Write(HtmlForm form, out int length);

    ValueTask<HtmlForm?> LoadAsync(HttpContext context, Page page);
}
