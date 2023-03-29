using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using WebFormsCore;
using WebFormsCore.Implementation;

[assembly: PreApplicationStartMethod(typeof(PageHandlerFactory), nameof(PageHandlerFactory.Start))]

namespace WebFormsCore;

public static class HttpContextExtensions
{
    public static IHttpContext CreateCoreContext(this HttpContext context)
    {
        if (context.Items["WebFormsCore.HttpContext"] is IHttpContext webFormsCoreContext)
        {
            return webFormsCoreContext;
        }

        var scope = PageHandlerFactory.Provider.CreateScope();
        var webFormsCoreContextImpl = PageHandlerFactory.ContextPool.Get();
        webFormsCoreContextImpl.SetHttpContext(context, scope.ServiceProvider);

        context.Items["WebFormsCore.Scope"] = scope;
        context.Items["WebFormsCore.HttpContext"] = webFormsCoreContextImpl;

        return webFormsCoreContextImpl;
    }

    public static IHttpContext CreateCoreContext(this HttpContext context, IServiceProvider provider)
    {
        if (context.Items["WebFormsCore.HttpContext"] is HttpContextImpl webFormsCoreContext)
        {
           webFormsCoreContext.RequestServices = provider;
           return webFormsCoreContext;
        }

        var webFormsCoreContextImpl = PageHandlerFactory.ContextPool.Get();
        webFormsCoreContextImpl.SetHttpContext(context, provider);
        context.Items["WebFormsCore.HttpContext"] = webFormsCoreContextImpl;

        return webFormsCoreContextImpl;
    }
}

public class PageHandlerFactory : HttpTaskAsyncHandler
{
    internal static readonly ObjectPool<HttpContextImpl> ContextPool = new DefaultObjectPool<HttpContextImpl>(new ContextPooledObjectPolicy());
    internal static IServiceProvider Provider;

    public static void Start()
    {
        DynamicModuleUtility.RegisterModule(typeof(LifeCycleModule));
    }

    public override async Task ProcessRequestAsync(HttpContext context)
    {
        if (Provider == null)
        {
            return;
        }

        await using var scope = Provider.CreateAsyncScope();

        var application = scope.ServiceProvider.GetRequiredService<IWebFormsApplication>();
        var path = application.GetPath(context.Request.Path);

        if (path == null)
        {
            return;
        }

        var coreContext = context.CreateCoreContext();

        await application.ProcessAsync(coreContext, path, context.Request.TimedOutToken);
    }

    private sealed class LifeCycleModule : IHttpModule
    {
        private static readonly object Lock = new();
        private static readonly SemaphoreSlim HostLock = new(1, 1);
        private bool _isInitialized;
        private static int _initializedModuleCount;

        public void Init(HttpApplication application)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (application is not null)
            {
                var wrapper = new EventHandlerTaskAsyncHelper(DisposeScopeAsync);
                application.AddOnEndRequestAsync(wrapper.BeginEventHandler, wrapper.EndEventHandler);
            }

            lock (Lock)
            {
                _initializedModuleCount++;

                if (_initializedModuleCount != 1 || _isInitialized)
                {
                    return;
                }

                _isInitialized = true;

                var services = new ServiceCollection();
                services.AddWebForms();
                services.AddLogging();

                var provider = services.BuildServiceProvider();
                Provider = provider;
                StartHostedServices(provider);
            }
        }

        private static async Task DisposeScopeAsync(object sender, EventArgs e)
        {
            var application = sender as HttpApplication;
            var context = application?.Context;

            if (context?.Items["WebFormsCore.Scope"] is IAsyncDisposable scope)
            {
                await scope.DisposeAsync();
            }

            if (context?.Items["WebFormsCore.HttpContext"] is HttpContextImpl httpContext)
            {
                ContextPool.Return(httpContext);
            }
        }

        public void Dispose()
        {
            lock (Lock)
            {
                _initializedModuleCount--;

                if (_initializedModuleCount != 0 || !_isInitialized)
                {
                    return;
                }

                _isInitialized = false;

                var provider = Provider;
                Provider = null;

                _ = Task.Run(async () =>
                {
                    await StopHostedServicesAsync(provider);

                    if (provider is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else if (provider is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                });
            }
        }

        private static void StartHostedServices(IServiceProvider provider)
        {
            var hostedServices = provider.GetServices<IHostedService>().ToArray();

            if (hostedServices.Length == 0)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                await HostLock.WaitAsync();

                try
                {
                    foreach (var hostedService in hostedServices)
                    {
                        await hostedService.StartAsync(default);
                    }
                }
                finally
                {
                    HostLock.Release();
                }
            });
        }

        private static async Task StopHostedServicesAsync(IServiceProvider provider)
        {
            var hostedServices = provider.GetServices<IHostedService>().ToArray();

            if (hostedServices.Length == 0)
            {
                return;
            }

            await HostLock.WaitAsync();

            try
            {
                foreach (var hostedService in hostedServices)
                {
                    await hostedService.StopAsync(default);
                }
            }
            finally
            {
                HostLock.Release();
            }
        }
    }

    private class ContextPooledObjectPolicy : IPooledObjectPolicy<HttpContextImpl>
    {
        public HttpContextImpl Create()
        {
            return new HttpContextImpl();
        }

        public bool Return(HttpContextImpl obj)
        {
            obj.Reset();
            return true;
        }
    }

}
