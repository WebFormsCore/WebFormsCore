using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Containers;
using WebFormsCore.Tests.EarlyHints.Pages;

namespace WebFormsCore.Tests.EarlyHints;

public class EarlyHintsTest(SeleniumFixture fixture)
{
    public static IEnumerable<object[]> BrowserData =>
    [
        [Browser.Chrome],
        [Browser.Firefox]
    ];

    [Theory, MemberData(nameof(BrowserData))]
    public async Task ScriptFetchedBeforePageLoad(Browser type)
    {
        await using var result = await fixture.StartAsync<EarlyHintsPage>(type, configureApp: app =>
        {
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path == "/script.js")
                {
                    await ctx.Response.WriteAsync("console.log('Hello World');");

                    if (ctx.RequestServices.GetService<IControlAccessor>()?.Control is EarlyHintsPage page)
                    {
                        page.MarkScriptLoaded();
                    }

                    return;
                }

                await next();
            });
        });

        Assert.True(result.Control.Page.EnableEarlyHints);
        Assert.True(result.Control.ScriptLoaded);
    }
}
