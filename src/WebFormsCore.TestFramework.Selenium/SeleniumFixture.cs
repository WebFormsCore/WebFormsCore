using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebFormsCore.UI;
using static WebFormsCore.SeleniumTest;

namespace WebFormsCore;

public sealed class SeleniumFixture : IDisposable
{
    private static readonly Lazy<string> ChromePath = new(() => new DriverManager().SetUpDriver(new ChromeConfig()));
    private static readonly Lazy<string> FirefoxPath = new(() => new DriverManager().SetUpDriver(new FirefoxConfig()));

    private readonly ConcurrentBag<IWebDriver> _chromeDrivers = [];
    private readonly ConcurrentBag<IWebDriver> _firefoxDrivers = [];

    public Task<ITestContext<TControl>> StartAsync<TControl>(
        Browser browser,
        Action<IServiceCollection>? configure = null,
        bool? headless = null)
        where TControl : Control, new()
    {
        headless ??= !Debugger.IsAttached;

        return browser switch
        {
            Browser.Chrome => StartChromeAsync<TControl>(configure, headless.Value),
            Browser.Firefox => StartFirefoxAsync<TControl>(configure, headless.Value),
            _ => throw new NotSupportedException(),
        };
    }

    public async Task<ITestContext<TControl>> StartChromeAsync<TControl>(
        Action<IServiceCollection>? configure = null,
        bool headless = true
    ) where TControl : Control, new()
    {
        return await StartBrowserAsync<TControl>(DriverFactory, configure);

        IWebDriver DriverFactory()
        {
            if (_chromeDrivers.TryTake(out var driver))
            {
                return driver;
            }

            var chromeOptions = new ChromeOptions();
            if (headless) chromeOptions.AddArgument("--headless");
            chromeOptions.AddArgument("--disable-search-engine-choice-screen");
            chromeOptions.AddArgument("--no-sandbox");

            return new ChromeDriver(ChromePath.Value, chromeOptions);
        }
    }

    public async Task<ITestContext<TControl>> StartFirefoxAsync<TControl>(
        Action<IServiceCollection>? configure = null,
        bool headless = true
    ) where TControl : Control, new()
    {
        return await StartBrowserAsync<TControl>(DriverFactory, configure);

        IWebDriver DriverFactory()
        {
            if (_firefoxDrivers.TryTake(out var driver))
            {
                return driver;
            }

            var firefoxOptions = new FirefoxOptions();
            firefoxOptions.SetEnvironmentVariable("MOZ_DISABLE_CONTENT_SANDBOX", "1");
            firefoxOptions.SetEnvironmentVariable("MOZ_DISABLE_GMP_SANDBOX", "1");
            firefoxOptions.SetEnvironmentVariable("MOZ_DISABLE_NPAPI_SANDBOX", "1");
            firefoxOptions.SetEnvironmentVariable("MOZ_DISABLE_GPU_SANDBOX", "1");
            firefoxOptions.SetEnvironmentVariable("MOZ_DISABLE_RDD_SANDBOX", "1");
            firefoxOptions.SetEnvironmentVariable("MOZ_DISABLE_SOCKET_PROCESS_SANDBOX", "1");
            if (headless) firefoxOptions.AddArgument("-headless");

            return new FirefoxDriver(FirefoxPath.Value, firefoxOptions);
        }
    }

    public Task<ITestContext<TControl>> StartBrowserAsync<TControl>(
        Func<IWebDriver> driverFactory,
        Action<IServiceCollection>? configure = null
    ) where TControl : Control, new()
    {
        return ControlTest.StartBrowserAsync(host => new SeleniumTestContext<TControl>(host, driverFactory(), this), configure);
    }

    internal bool ReturnDriver(IWebDriver driver)
    {
        switch (driver)
        {
            case ChromeDriver chromeDriver:
                _chromeDrivers.Add(chromeDriver);
                return true;
            case FirefoxDriver firefoxDriver:
                _firefoxDrivers.Add(firefoxDriver);
                return true;
            default:
                return false;
        }
    }

    public void Dispose()
    {
        foreach (var driver in _chromeDrivers)
        {
            try
            {
                driver.Dispose();
            }
            catch
            {
                // Ignore
            }
        }

        foreach (var driver in _firefoxDrivers)
        {
            try
            {
                driver.Dispose();
            }
            catch
            {
                // Ignore
            }
        }

#if NET
        _chromeDrivers.Clear();
        _firefoxDrivers.Clear();
#else
        while (_chromeDrivers.TryTake(out _)) ;
        while (_firefoxDrivers.TryTake(out _)) ;
#endif
    }
}