using System;
using System.Web;
using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore.Abstractions;

public interface IHttpServiceProviderFactory
{
    IServiceProvider CreateRootProvider(HttpApplication application);

    IServiceScope CreateScope(HttpContext context, IServiceProvider rootScope);
}
