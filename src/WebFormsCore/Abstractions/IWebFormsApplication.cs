using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace WebFormsCore
{
    public interface IWebFormsApplication
    {
        Task<bool> ProcessAsync(HttpContext context, IServiceProvider provider, CancellationToken token);
    }
}
