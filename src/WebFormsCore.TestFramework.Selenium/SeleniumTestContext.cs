using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using WebFormsCore.Internal;
using WebFormsCore.UI;

namespace WebFormsCore;

internal class SeleniumTestContext<T>(IHost host, IWebDriver driver) : WebServerContext<T>(host)
    where T : Control, new()
{
    public override async Task GoToUrlAsync(string url)
    {
        await driver.Navigate().GoToUrlAsync(url);
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

    protected override ValueTask DisposeCoreAsync()
    {
        driver.Dispose();
        return ValueTask.CompletedTask;
    }
}