using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebFormsCore.Features;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
using Page = WebFormsCore.UI.Page;

namespace WebFormsCore;

public class ControlTest
{
    public static async Task<ITestContext<TControl>> StartBrowserAsync<TControl>(
        Func<IWebHost, WebServerContext<TControl>> contextFactory,
        Action<IServiceCollection>? configure = null,
        Action<IApplicationBuilder>? configureApp = null,
        bool enableViewState = true
    ) where TControl : Control, new()
    {
        var builder = WebHost.CreateDefaultBuilder();
        StrongBox<WebServerContext<TControl>> context = new StrongBox<WebServerContext<TControl>>();

        builder.UseKestrel(options =>
        {
            options.Limits.MaxResponseBufferSize = null;
            options.Listen(IPAddress.Loopback, 0);
        });

        builder.ConfigureServices(services =>
        {
            services.AddWebFormsCore();

            var typeProvider = typeof(TControl).Assembly.GetCustomAttribute<AssemblyControlTypeProviderAttribute>()?.Type;

            if (typeProvider is null)
            {
                throw new InvalidOperationException("Type provider not found");
            }

            services.AddSingleton(typeof(IControlTypeProvider), typeProvider);
            services.AddSingleton<IWebFormsEnvironment, TestEnvironment>();

            services.AddOptions<ViewStateOptions>()
                .Configure(options =>
                {
                    options.Enabled = enableViewState;
                });

            configure?.Invoke(services);
        });

        builder.Configure(app =>
        {
            configureApp?.Invoke(app);

            app.Run(async ctx =>
            {
                try
                {
                    if (ctx.Request.Path == "/favicon.ico")
                    {
                        ctx.Response.StatusCode = 404;
                        return;
                    }

                    Debug.Assert(context.Value != null);
                    ctx.Features.Set<ITestContextFeature>(new TestContextFeature(context.Value!));

                    var activator = ctx.RequestServices.GetRequiredService<IWebObjectActivator>();
                    var control = activator.CreateControl<TControl>();

                    var responseBody = ctx.Response.Body;
                    using var memoryStream = new MemoryStream();
                    ctx.Response.Body = memoryStream;

                    if (control is Page page)
                    {
                        await ctx.ExecutePageAsync(page);
                    }
                    else
                    {
                        page = new Page();

                        var literal = activator.CreateControl<LiteralControl>();
                        literal.Text = "<!DOCTYPE html>";
                        page.Controls.AddWithoutPageEvents(literal);
                        page.Controls.AddWithoutPageEvents(activator.CreateControl<HtmlHead>());

                        var body = activator.CreateControl<HtmlBody>();
                        body.Controls.AddWithoutPageEvents(control);

                        page.Controls.AddWithoutPageEvents(body);

                        await ctx.ExecutePageAsync(page);
                    }

                    ctx.Response.ContentLength = memoryStream.Length;
                    memoryStream.Position = 0;

                    await memoryStream.CopyToAsync(responseBody);
#if NET
                    await ctx.Response.CompleteAsync();
#endif
                    await responseBody.FlushAsync();

                    var transport = ctx.Features.Get<IConnectionTransportFeature>()!.Transport;
#if NET
                    await transport.Output.CompleteAsync();
#else
                    transport.Output.Complete();
                    await transport.Output.FlushAsync();
#endif

                    await context.Value!.SetControlAsync(control);
                }
                catch (Exception e)
                {
                    context.Value!.SetException(e);
                }
            });
        });

        var host = builder.Build();

        context.Value = contextFactory(host);

        await host.StartAsync();
        await context.Value.GoToUrlAsync(context.Value.Url);

        return context.Value;
    }
}

public interface ITestContext<out T> : ITestContext, IAsyncDisposable
    where T : Control
{
    T Control { get; }
}