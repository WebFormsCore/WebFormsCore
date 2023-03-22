#if OWIN
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Owin;
using WebFormsCore.Example;

[assembly: OwinStartup(typeof(Startup))]

namespace WebFormsCore.Example
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var services = new ServiceCollection();

            services.UseOwinWebForms();

            app.Use<WebFormsCoreMiddleware>(services);
        }
    }
}
#endif
