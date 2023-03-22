using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Implementation;

namespace WebFormsCore;

using AppFunc = Func<IDictionary<string, object>, Task>;

public class WebFormsCoreMiddleware
{
    private readonly AppFunc _next;
    private readonly IServiceProvider _serviceProvider;

    public WebFormsCoreMiddleware(AppFunc next)
    {
        var services = new ServiceCollection();
        services.UseOwinWebForms();
        _serviceProvider = services.BuildServiceProvider();
        _next = next;
    }

    public WebFormsCoreMiddleware(AppFunc next, IServiceCollection services)
    {
        _serviceProvider = services.BuildServiceProvider();
        _next = next;
    }

    public WebFormsCoreMiddleware(AppFunc next, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _next = next;
    }

    public async Task Invoke(IDictionary<string, object> env)
    {
        if (env["owin.RequestPath"] is not string envPath)
        {
            await _next(env);
            return;
        }

        await using var scope = _serviceProvider.CreateAsyncScope();
        var application = scope.ServiceProvider.GetRequiredService<IWebFormsApplication>();
        var path = application.GetPath(envPath);

        if (path == null)
        {
            await _next(env);
            return;
        }

        var context = new HttpContextImpl(); // TODO: Pooling
        context.SetHttpContext(env, scope.ServiceProvider);

        await application.ProcessAsync(context, path, context.RequestAborted);
    }
}
