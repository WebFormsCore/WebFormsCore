using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebFormsCore.Containers;
using WebFormsCore.Features;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
using Page = WebFormsCore.UI.Page;

namespace WebFormsCore;

public class ControlTest
{
 private const string Password = "UnitTest";
    private static readonly X509Certificate2 Certificate;

    static ControlTest()
    {
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddDnsName(Environment.MachineName);

        var distinguishedName = new X500DistinguishedName("CN=localhost");

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature , false));

        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
        request.CertificateExtensions.Add(sanBuilder.Build());

        var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));

    #if NET9_0_OR_GREATER
        Certificate = X509CertificateLoader.LoadPkcs12(
            certificate.Export(X509ContentType.Pfx, Password),
            Password,
            X509KeyStorageFlags.PersistKeySet
        );
#else
        Certificate = new X509Certificate2(
            certificate.Export(X509ContentType.Pfx, Password),
            Password,
            X509KeyStorageFlags.PersistKeySet
        );
#endif
    }

    public static async Task<ITestContext<TControl>> StartBrowserAsync<TControl>(
        Func<IHost, Task<WebServerContext<TControl>>> contextFactory,
        Action<IServiceCollection>? configure = null,
        Action<IApplicationBuilder>? configureApp = null,
        bool enableViewState = true,
        HttpProtocols protocols = HttpProtocols.Http1AndHttp2
    ) where TControl : Control, new()
    {
        var builder = Host.CreateDefaultBuilder();
        var context = new StrongBox<WebServerContext<TControl>>();

        builder.ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseKestrel(options =>
            {
                options.Limits.MaxResponseBufferSize = null;
                options.Listen(IPAddress.Loopback, 0, listenOptions =>
                {
                    listenOptions.UseHttps(Certificate);
                    listenOptions.Protocols = protocols;
                });
            });

            webBuilder.ConfigureServices(services =>
            {
                services.AddWebFormsCore();

                var typeProvider = typeof(TControl).Assembly.GetCustomAttribute<AssemblyControlTypeProviderAttribute>()?.Type;

                if (typeProvider is null)
                {
                    throw new InvalidOperationException("Type provider not found");
                }

                services.AddSingleton(typeof(IControlTypeProvider), typeProvider);
                services.AddSingleton<IWebFormsEnvironment, TestEnvironment>();
                services.AddSingleton<IControlAccessor, ControlAccessor>();

                services.AddOptions<ViewStateOptions>()
                    .Configure(options =>
                    {
                        options.Enabled = enableViewState;
                    });

                configure?.Invoke(services);
            });

            webBuilder.Configure(app =>
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

                        if (ctx.RequestServices.GetRequiredService<IControlAccessor>() is not ControlAccessor controlAccessor)
                        {
                            throw new InvalidOperationException("Control accessor cannot be overridden.");
                        }

                        controlAccessor.Control = control;

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

                        ctx.Response.OnCompleted(async () =>
                        {
                            // For some reason, ASP.NET Core decides to leave the streams open when calling OnCompleted.
                            // The browser waits until the connection is closed before the 'fetch' promise is resolved.
                            // We need to close the streams manually.
                            await ctx.Response.CompleteAsync();

                            await context.Value!.SetControlAsync(control);
                            controlAccessor.Control = null!;
                        });
                    }
                    catch (Exception e)
                    {
                        context.Value!.SetException(e);
                    }
                });
            });
        });

        var host = builder.Build();

        context.Value = await contextFactory(host);

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