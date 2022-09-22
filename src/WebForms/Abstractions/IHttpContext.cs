using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;

namespace System.Web
{
    public static class HttpContextAccessor
    {
        private static readonly AsyncLocal<HttpContextHolder> HttpContextCurrent = new();

        public static IHttpContext? Current
        {
            get => HttpContextCurrent.Value?.Context;
            set
            {
                var holder = HttpContextCurrent.Value;
                if (holder != null)
                {
                    // Clear current HttpContext trapped in the AsyncLocals, as its done.
                    holder.Context = null;
                }

                if (value != null)
                {
                    // Use an object indirection to hold the HttpContext in the AsyncLocal,
                    // so it can be cleared in all ExecutionContexts when its cleared.
                    HttpContextCurrent.Value = new HttpContextHolder { Context = value };
                }
            }
        }

        private class HttpContextHolder
        {
            public IHttpContext? Context;
        }
    }

    public interface IHttpContext
    {
        IServiceProvider RequestServices { get; }

        IHttpRequest Request { get; }

        IHttpResponse Response { get; }

        CancellationToken RequestAborted { get; }

        IDictionary Items { get;}

        IPrincipal User { get; }
    }
}
