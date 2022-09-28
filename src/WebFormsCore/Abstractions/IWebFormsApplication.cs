using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace WebFormsCore
{
    public interface IWebFormsApplication
    {
        string? GetPath(HttpContext context);

        Task<bool> ProcessAsync(HttpContext context, string path, IServiceProvider provider, CancellationToken token);
    }
}
