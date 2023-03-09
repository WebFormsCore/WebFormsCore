using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using WebFormsCore.UI;

namespace WebFormsCore
{
    public interface IWebFormsApplication
    {
        string? GetPath(HttpContext context);

        Task<Page> ProcessAsync(HttpContext context, string path, IServiceProvider provider, CancellationToken token);
    }
}
