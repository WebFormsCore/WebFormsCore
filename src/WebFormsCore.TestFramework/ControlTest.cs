using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
using Page = WebFormsCore.UI.Page;

namespace WebFormsCore;

public class ControlTest
{
    public static async Task<ITestContext<TControl>> StartBrowserAsync<TControl, TTypeProvider>(
        Func<WebApplication, WebServerContext<TControl>> contextFactory,
        Action<IServiceCollection>? configure = null,
        bool enableViewState = true
    )
        where TControl : Control, new()
        where TTypeProvider : class, IControlTypeProvider
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());

        builder.WebHost.UseKestrelCore();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
        });

        builder.Services.AddWebFormsCore();

        builder.Services.AddSingleton<IControlTypeProvider, TTypeProvider>();
        builder.Services.AddSingleton<IWebFormsEnvironment, TestEnvironment>();

        builder.Services.AddOptions<ViewStateOptions>()
            .Configure(options =>
            {
                options.Enabled = enableViewState;
            });

        configure?.Invoke(builder.Services);

        var host = builder.Build();
        var context = contextFactory(host);

        host.UseWebFormsCore();
        host.Run(async ctx =>
        {
            var activator = host.Services.GetRequiredService<IWebObjectActivator>();
            var control = activator.CreateControl<TControl>();

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

            await ctx.Response.CompleteAsync();
            await ctx.Response.Body.FlushAsync();

            var transport = ctx.Features.GetRequiredFeature<IConnectionTransportFeature>().Transport;
            await transport.Output.CompleteAsync();

            await context.SetControlAsync(control);
        });

        await host.StartAsync();
        await context.GoToUrlAsync(context.Url);

        return context;
    }
}

public interface ITestContext<out T> : ITestContext, IAsyncDisposable
    where T : Control
{
    T Control { get; }
}