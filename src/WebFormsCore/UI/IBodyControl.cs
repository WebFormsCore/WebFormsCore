using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI;

public interface IBodyControl
{
    Task RenderInBodyAsync(HtmlTextWriter writer, CancellationToken token);
}
