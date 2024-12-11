using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebFormsCore.UI;

namespace WebFormsCore
{
    public interface IWebFormsApplication
    {
        string? GetPath(string path);

        Task<Page> ProcessAsync(HttpContext context, string path, CancellationToken token);
    }
}
