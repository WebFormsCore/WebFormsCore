using System.Collections.Specialized;
using System.IO;

namespace System.Web
{
    public interface IHttpRequest
    {
        IHttpContext HttpContext { get; }

        string HttpMethod { get; }

        string? Path { get; }

        NameValueCollection Form { get; }

        NameValueCollection QueryString { get; }

        NameValueCollection Headers { get; }

        string? ContentType { get; }

        Stream InputStream { get; }
    }
}
