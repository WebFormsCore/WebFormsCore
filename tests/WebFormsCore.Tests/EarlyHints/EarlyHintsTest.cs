using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using WebFormsCore.Tests.EarlyHints.Pages;

namespace WebFormsCore.Tests.EarlyHints;

public class EarlyHintsTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task EventsCalled(Browser type)
    {
        await using var result = await fixture.StartAsync<EarlyHintsPage>(type, configureApp: app =>
        {
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path == "/script.js")
                {
                    await ctx.Response.WriteAsync("console.log('Hello World');");
                    return;
                }

                await next();
            });
        });

        Assert.True(result.Control.Page.EnableEarlyHints);

        // We cannot check if the browser received the early hints
        // so we only check if the hints were set
        Assert.True(result.Control.Context.Items.TryGetValue("EarlyHints", out var hints));
        Assert.NotNull(hints);
        Assert.Contains("script.js", hints.ToString());
    }
}
