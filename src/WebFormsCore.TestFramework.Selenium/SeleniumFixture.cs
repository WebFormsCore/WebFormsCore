using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    private static readonly Lazy<string> ChromePath = new(() => new DriverManager().SetUpDriver(new ChromeConfig(), "MatchingBrowser"));
    private static readonly Lazy<string> FirefoxPath = new(() => new DriverManager().SetUpDriver(new FirefoxConfig()));

    private readonly Context _chromeDrivers = new();
    private readonly Context _firefoxDrivers = new();

    public Task<ITestContext<TControl>> StartAsync<TControl>(
        Browser browser,
        Action<IServiceCollection>? configure = null,
        Action<IApplicationBuilder>? configureApp = null,
        bool? headless = null,
        bool enableViewState = true,
        HttpProtocols protocols = HttpProtocols.Http1AndHttp2)
        where TControl : Control, new()
    {
        headless ??= !Debugger.IsAttached;

        return browser switch
        {
            Browser.Chrome => StartChromeAsync<TControl>(configure, configureApp, headless.Value, enableViewState, protocols),
            Browser.Firefox => StartFirefoxAsync<TControl>(configure, configureApp, headless.Value, enableViewState, protocols),
            _ => throw new NotSupportedException(),
        };
    }

    public async Task<ITestContext<TControl>> StartChromeAsync<TControl>(
        Action<IServiceCollection>? configure = null,
        Action<IApplicationBuilder>? configureApp = null,
        bool headless = true,
        bool enableViewState = true,
        HttpProtocols protocols = HttpProtocols.Http1AndHttp2
    ) where TControl : Control, new()
    {
        return await StartBrowserAsync<TControl>(_chromeDrivers, DriverFactory, configure, configureApp, enableViewState, protocols);

        IWebDriver DriverFactory()
        {
            var chromeOptions = new ChromeOptions();
            if (headless) chromeOptions.AddArgument("--headless");
            chromeOptions.AddArgument("--disable-search-engine-choice-screen");
            chromeOptions.AddArgument("--no-sandbox");
            chromeOptions.AddArgument("--ignore-certificate-errors");

            return new ChromeDriver(ChromePath.Value, chromeOptions);
        }
    }

    public async Task<ITestContext<TControl>> StartFirefoxAsync<TControl>(
        Action<IServiceCollection>? configure = null,
        Action<IApplicationBuilder>? configureApp = null,
        bool headless = true,
        bool enableViewState = true,
        HttpProtocols protocols = HttpProtocols.Http1AndHttp2
    ) where TControl : Control, new()
    {
        return await StartBrowserAsync<TControl>(_firefoxDrivers, DriverFactory, configure, configureApp, enableViewState, protocols);

        IWebDriver DriverFactory()
        {
            if (_firefoxDrivers.Drivers.TryTake(out var driver))
            {
                return driver;
            }

            var firefoxOptions = new FirefoxOptions();
            firefoxOptions.AcceptInsecureCertificates = true;
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

    private Task<ITestContext<TControl>> StartBrowserAsync<TControl>(
        Context context,
        Func<IWebDriver> driverFactory,
        Action<IServiceCollection>? configure = null,
        Action<IApplicationBuilder>? configureApp = null,
        bool enableViewState = true,
        HttpProtocols protocols = HttpProtocols.Http1AndHttp2
    ) where TControl : Control, new()
    {
        return ControlTest.StartBrowserAsync(CreateDriver, configure, configureApp, enableViewState, protocols);

        async Task<WebServerContext<TControl>> CreateDriver(IHost host)
        {
            await context.Semaphore.WaitAsync(TimeSpan.FromSeconds(30));

            if (!context.Drivers.TryTake(out var driver))
            {
                driver = driverFactory();
            }

            return new SeleniumTestContext<TControl>(host, driver, context);
        }
    }

    public void Dispose()
    {
        _chromeDrivers.Clear();
        _firefoxDrivers.Clear();
    }

    internal class Context
    {
        public readonly ConcurrentBag<IWebDriver> Drivers = [];
        public readonly SemaphoreSlim Semaphore = new(4); // Limit to 4 concurrent drivers

        public void Clear()
        {
            while (Drivers.TryTake(out var driver))
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
        }
    }
}