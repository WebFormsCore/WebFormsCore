using System;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Abstractions;

namespace WebFormsCore;

public class DefaultHttpServiceProviderFactory : IHttpServiceProviderFactory
{
    public IServiceProvider CreateRootProvider(HttpApplication application)
    {
        var services = new ServiceCollection();
        services.AddWebForms();
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    public IServiceScope CreateScope(HttpContext context, IServiceProvider rootScope)
    {
        return rootScope.CreateScope();
    }
}
