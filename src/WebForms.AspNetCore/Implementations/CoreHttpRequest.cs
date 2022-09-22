using System.Collections.Specialized;
using System.Web;

namespace WebForms.AspNetCore
{
    public class CoreHttpRequest : IHttpRequest
    {
        private readonly HttpRequest _request;

        public CoreHttpRequest(HttpRequest request, IHttpContext httpContext)
        {
            _request = request;
            HttpContext = httpContext;
        }

        public IHttpContext HttpContext { get; }

        public string HttpMethod => _request.HttpMethod;

        public string? Path => _request.Path;

        public NameValueCollection Form => _request.Form;

        public NameValueCollection QueryString => _request.QueryString;

        public NameValueCollection Headers => _request.Headers;

        public string? ContentType => _request.ContentType;

        public Stream InputStream => _request.InputStream;
    }
}
