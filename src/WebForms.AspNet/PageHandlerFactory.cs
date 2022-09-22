using System;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using WebFormsCore;

[assembly: PreApplicationStartMethod(typeof(PageHandlerFactory), nameof(PageHandlerFactory.Start))]

namespace WebFormsCore
{

    public class PageHandlerFactory : HttpTaskAsyncHandler
    {
        private static IServiceProvider _provider;
        
        public static void Start()
        {
            DynamicModuleUtility.RegisterModule(typeof(LifeCycleModule));
        }

        public override async Task ProcessRequestAsync(HttpContext context)
        {
            if (_provider == null)
            {
                return;
            }

            await using var scope = _provider.CreateAsyncScope();

            var application = scope.ServiceProvider.GetRequiredService<IWebFormsApplication>();

            await application.ProcessAsync(context, scope.ServiceProvider, context.Request.TimedOutToken);
        }

        private sealed class LifeCycleModule : IHttpModule
        {
            private static readonly object Lock = new();
            private bool _isInitialized;
            private static int _initializedModuleCount;

            public void Init(HttpApplication context)
            {
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
                    _provider = services.BuildServiceProvider();
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

                    var disposable = _provider as IDisposable;
                    _provider = null;
                    disposable?.Dispose();
                }
            }
        }
    }
}
