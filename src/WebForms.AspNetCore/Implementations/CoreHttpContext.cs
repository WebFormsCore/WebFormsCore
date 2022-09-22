using System.Collections;
using System.Security.Principal;
using System.Web;

namespace WebForms.AspNetCore
{
    internal class CoreHttpContext : IHttpContext
    {
        private readonly HttpContext _context;

        public CoreHttpContext(HttpContext context, IServiceProvider provider, CancellationToken cancellationToken)
        {
            _context = context;
            RequestServices = provider;
            RequestAborted = cancellationToken;
            Request = new CoreHttpRequest(context.Request, this);
            Response = new CoreHttpResponse(context.Response, this);
        }

        public IServiceProvider RequestServices { get; }

        public IHttpRequest Request { get; }

        public IHttpResponse Response { get; }

        public CancellationToken RequestAborted { get; }


        public IDictionary Items => _context.Items;

        public IPrincipal User => _context.User;
    }
}
