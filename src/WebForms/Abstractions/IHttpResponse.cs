using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Web
{
    public interface IHttpResponse
    {
        IHttpContext HttpContext { get; }

        int StatusCode { get; set; }
    }
}
