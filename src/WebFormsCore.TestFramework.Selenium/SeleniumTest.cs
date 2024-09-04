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
    public static async Task<ITestContext<TControl>> StartChromeAsync<TControl, TTypeProvider>(
        Action<IServiceCollection>? configure = null,
        bool enableViewState = true,
        bool headless = true
    )
        where TControl : Control, new()
        where TTypeProvider : class, IControlTypeProvider
    {
        return await StartBrowserAsync<TControl, TTypeProvider>(DriverFactory, configure, enableViewState);

        IWebDriver DriverFactory()
        {
            var path = new DriverManager().SetUpDriver(new ChromeConfig());
            var chromeOptions = new ChromeOptions();
            if (headless) chromeOptions.AddArgument("headless");
            chromeOptions.AddArguments("disable-search-engine-choice-screen");

            return new ChromeDriver(path, chromeOptions);
        }
    }

    public static async Task<ITestContext<TControl>> StartFirefoxAsync<TControl, TTypeProvider>(
        Action<IServiceCollection>? configure = null,
        bool enableViewState = true,
        bool headless = true
    )
        where TControl : Control, new()
        where TTypeProvider : class, IControlTypeProvider
    {
        return await StartBrowserAsync<TControl, TTypeProvider>(DriverFactory, configure, enableViewState);

        IWebDriver DriverFactory()
        {
            var path = new DriverManager().SetUpDriver(new FirefoxConfig());
            var firefoxOptions = new FirefoxOptions();
            if (headless) firefoxOptions.AddArgument("--headless");

            return new FirefoxDriver(path, firefoxOptions);
        }
    }

    public static Task<ITestContext<TControl>> StartBrowserAsync<TControl, TTypeProvider>(
        Func<IWebDriver> driverFactory,
        Action<IServiceCollection>? configure = null,
        bool enableViewState = true
    )
        where TControl : Control, new()
        where TTypeProvider : class, IControlTypeProvider
    {
        return ControlTest.StartBrowserAsync<TControl, TTypeProvider>(
            host => new SeleniumTestContext<TControl>(host, driverFactory()),
            configure,
            enableViewState);
    }
}
