using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Containers;
using WebFormsCore.Tests.EarlyHints.Pages;

namespace WebFormsCore.Tests.EarlyHints;

public class EarlyHintsTest(SeleniumFixture fixture)
{
    public static IEnumerable<object[]> BrowserData =>
    [
        // Chrome only supports HTTP/2
        [Browser.Chrome, HttpProtocols.Http2],
        [Browser.Firefox, HttpProtocols.Http2],
        [Browser.Firefox, HttpProtocols.Http1]
    ];

    [Theory, MemberData(nameof(BrowserData))]
    public async Task ScriptFetchedBeforePageLoad(Browser type, HttpProtocols protocols)
    {
        var loaded = new StrongBox<bool>();

        await using var result = await fixture.StartAsync<EarlyHintsPage>(type, protocols: protocols, configureApp: app =>
        {
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path != "/script.js")
                {
                    await next();
                }
                else
                {
                    if (ctx.RequestServices.GetService<IControlAccessor>()?.Control is EarlyHintsPage page)
                    {
                        page.MarkScriptLoaded();
                    }

                    ctx.Response.ContentType = "application/javascript";
                    loaded.Value = true;
                }
            });
        });

        Assert.True(result.Control.Page.EnableEarlyHints);
        Assert.True(loaded.Value);
    }
}
