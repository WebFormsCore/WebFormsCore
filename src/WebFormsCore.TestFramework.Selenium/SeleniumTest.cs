using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
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

    public static Task<ITestContext<TControl>> StartAsync<TControl>(
        Browser browser,
        Action<IServiceCollection>? configure = null,
        bool headless = true)
        where TControl : Control, new()
    {
        return browser switch
        {
            Browser.Chrome => StartChromeAsync<TControl>(configure, headless),
            Browser.Firefox => StartFirefoxAsync<TControl>(configure, headless),
            _ => throw new NotSupportedException(),
        };
    }

    public static async Task<ITestContext<TControl>> StartChromeAsync<TControl>(
        Action<IServiceCollection>? configure = null,
        bool headless = true
    ) where TControl : Control, new()
    {
        return await StartBrowserAsync<TControl>(DriverFactory, configure);

        IWebDriver DriverFactory()
        {
            var chromeOptions = new ChromeOptions();
            if (headless) chromeOptions.AddArgument("headless");
            chromeOptions.AddArgument("disable-search-engine-choice-screen");
            chromeOptions.AddArgument("no-sandbox");

            return new ChromeDriver(ChromePath.Value, chromeOptions);
        }
    }

    public static async Task<ITestContext<TControl>> StartFirefoxAsync<TControl>(
        Action<IServiceCollection>? configure = null,
        bool headless = true
    ) where TControl : Control, new()
    {
        return await StartBrowserAsync<TControl>(DriverFactory, configure);

        IWebDriver DriverFactory()
        {
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

    public static Task<ITestContext<TControl>> StartBrowserAsync<TControl>(
        Func<IWebDriver> driverFactory,
        Action<IServiceCollection>? configure = null
    ) where TControl : Control, new()
    {
        return ControlTest.StartBrowserAsync(ContextFactory, configure);

        WebServerContext<TControl> ContextFactory(WebApplication host)
        {
            return new SeleniumTestContext<TControl>(host, driverFactory());
        }
    }

    public enum Browser
    {
        Chrome,
        Firefox,
    }

    public class BrowserData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [Browser.Chrome];
            yield return [Browser.Firefox];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
