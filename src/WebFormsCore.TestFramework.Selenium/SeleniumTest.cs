using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebFormsCore.UI;

namespace WebFormsCore;

public static class SeleniumTest
{
    private static readonly Lazy<string> ChromePath = new(() => new DriverManager().SetUpDriver(new ChromeConfig()));
    private static readonly Lazy<string> FirefoxPath = new(() => new DriverManager().SetUpDriver(new FirefoxConfig()));

    public static async Task<ITestContext<TControl>> StartChromeAsync<TControl>(
        Action<IServiceCollection>? configure = null,
        bool enableViewState = true,
        bool headless = true
    ) where TControl : Control, new()
    {
        return await StartBrowserAsync<TControl>(DriverFactory, configure, enableViewState);

        IWebDriver DriverFactory()
        {
            var chromeOptions = new ChromeOptions();
            if (headless) chromeOptions.AddArgument("headless");
            chromeOptions.AddArguments("disable-search-engine-choice-screen");

            return new ChromeDriver(ChromePath.Value, chromeOptions);
        }
    }

    public static async Task<ITestContext<TControl>> StartFirefoxAsync<TControl>(
        Action<IServiceCollection>? configure = null,
        bool enableViewState = true,
        bool headless = true
    ) where TControl : Control, new()
    {
        return await StartBrowserAsync<TControl>(DriverFactory, configure, enableViewState);

        IWebDriver DriverFactory()
        {
            var firefoxOptions = new FirefoxOptions();
            if (headless) firefoxOptions.AddArgument("--headless");

            return new FirefoxDriver(FirefoxPath.Value, firefoxOptions);
        }
    }

    public static Task<ITestContext<TControl>> StartBrowserAsync<TControl>(
        Func<IWebDriver> driverFactory,
        Action<IServiceCollection>? configure = null,
        bool enableViewState = true
    ) where TControl : Control, new()
    {
        return ControlTest.StartBrowserAsync<TControl>(
            host => new SeleniumTestContext<TControl>(host, driverFactory()),
            configure,
            enableViewState);
    }
}
