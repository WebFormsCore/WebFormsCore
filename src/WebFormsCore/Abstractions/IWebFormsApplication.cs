using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using WebFormsCore.UI;

namespace WebFormsCore
{
    public interface IWebFormsApplication
    {
        string? GetPath(string path);

        Task<Page> ProcessAsync(IHttpContext context, string path, CancellationToken token);
    }
}
