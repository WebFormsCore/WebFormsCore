using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using WebFormsCore.UI;

namespace WebFormsCore;

internal class SeleniumTestContext<T>(IHost host, IWebDriver driver) : WebServerContext<T>(host)
    where T : Control, new()
{
    public override async Task GoToUrlAsync(string url)
    {
        await driver.Navigate().GoToUrlAsync(url);
    }

    public override async Task ReloadAsync()
    {
        await driver.Navigate().RefreshAsync();
    }

    public override IElement GetElementById(string id)
    {
        return new SeleniumElement(driver.FindElement(By.Id(id)));
    }

    protected override ValueTask DisposeCoreAsync()
    {
        driver.Dispose();
        return ValueTask.CompletedTask;
    }
}