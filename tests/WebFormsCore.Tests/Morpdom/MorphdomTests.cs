using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Tests.Morphdom.Pages;

namespace WebFormsCore.Tests.Morphdom;

public class MorphdomTests(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task DocumentTitle(Browser type)
    {
        await using var result = await fixture.StartAsync<TitlePage>(type);

        Assert.Equal("Success", await result.ExecuteScriptAsync("return document.title"));

        await result.Control.btnSubmit.ClickAsync();

        Assert.Equal("Success", await result.ExecuteScriptAsync("return document.title"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task UpdateScript(Browser type)
    {
        await using var result = await fixture.StartAsync<UpdateScriptPage>(type, new SeleniumFixtureOptions
        {
            Configure = services =>
            {
                services.Configure<WebFormsCoreOptions>(options =>
                {
                    options.RenderScriptOnPostBack = true;
                });
            }
        });

        Assert.Equal("null", await result.ExecuteScriptAsync("return window.counter ?? 'null'"));

        await result.Control.btnSetScript.ClickAsync();

        Assert.Equal("1", await result.ExecuteScriptAsync("return window.counter"));

        await result.Control.btnSetScript.ClickAsync();

        Assert.Equal("1", await result.ExecuteScriptAsync("return window.counter"));
    }
}
