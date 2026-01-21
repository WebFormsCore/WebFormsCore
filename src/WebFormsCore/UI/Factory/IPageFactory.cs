using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebFormsCore.UI;

public interface IPageFactory
{
    /// <summary>
    /// Creates a Page instance for the specified Control.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="control">The control to create a Page for.</param>
    Task<Page> CreatePageForControlAsync(HttpContext context, Control control);
}
