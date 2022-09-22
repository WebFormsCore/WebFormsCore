using System.Web;

namespace WebForms.AspNetCore
{
    internal class CoreHttpResponse : IHttpResponse
    {
        private readonly HttpResponse _response;

        public CoreHttpResponse(HttpResponse response, IHttpContext context)
        {
            _response = response;
            HttpContext = context;
        }

        public IHttpContext HttpContext { get; }

        public int StatusCode
        {
            get => _response.StatusCode;
            set => _response.StatusCode = value;
        }
    }
}
