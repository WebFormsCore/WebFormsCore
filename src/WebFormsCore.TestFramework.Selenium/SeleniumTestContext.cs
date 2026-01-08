using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using WebFormsCore.Internal;
using WebFormsCore.UI;

namespace WebFormsCore;

internal class SeleniumTestContext<T>(
    IHost host,
    IWebDriver driver,
    SeleniumFixture.Context context
) : WebServerContext<T>(host)
    where T : Control, new()
{
    public override async Task GoToUrlAsync(string url)
    {
        await driver.Navigate().GoToUrlAsync(url);

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
        wait.Until(static d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
    }

    public override ValueTask<string> GetHtmlAsync()
    {
        return new ValueTask<string>(driver.PageSource);
    }

    public override async ValueTask ReloadAsync()
    {
        await driver.Navigate().RefreshAsync();
    }

    public override IElement? GetElementById(string id)
    {
        try
        {
            return new SeleniumElement(driver.FindElement(By.Id(id)));
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    public override IElement? QuerySelector(string selector)
    {
        try
        {
            return new SeleniumElement(driver.FindElement(By.CssSelector(selector)));
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    public override IAsyncEnumerable<IElement> QuerySelectorAll(string selector)
    {
        return driver.FindElements(By.CssSelector(selector))
            .Select(IElement (element) => new SeleniumElement(element))
            .AsAsyncEnumerable();
    }

    public override ValueTask<string?> ExecuteScriptAsync(string script)
    {
        return new ValueTask<string?>(((IJavaScriptExecutor)driver).ExecuteScript(script)?.ToString());
    }

    protected override ValueTask DisposeCoreAsync()
    {
        context.Drivers.Add(driver);
        context.Semaphore.Release();
        return base.DisposeCoreAsync();
    }
}