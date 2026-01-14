using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using WebFormsCore.UI;
using static WebFormsCore.SeleniumTest;

namespace WebFormsCore;

public class CurrentState<T> : IAsyncDisposable
{
    public T State { get; set; } = default!;

    public ITestContext Browser { get; set; } = null!;

    public async ValueTask DisposeAsync()
    {
        await Browser.DisposeAsync();
    }
}

internal class TestControl<TState>(Func<Control, Task<TState>> stateFactory, CurrentState<TState> stateContainer) : Control
{
    protected override async ValueTask OnPreInitAsync(CancellationToken token)
    {
        stateContainer.State = await stateFactory(this);

        await base.OnPreInitAsync(token);
    }
}

public sealed class SeleniumFixture : IDisposable
{
    private readonly Context _chromeDrivers = new();
    private readonly Context _firefoxDrivers = new();

    public async Task<CurrentState<TControl>> StartAsync<TControl>(
        Browser browser,
        Func<TControl> stateFactory,
        Action<IServiceCollection>? configure = null,
        Action<IApplicationBuilder>? configureApp = null,
        bool? headless = null,
        bool enableViewState = true,
        HttpProtocols protocols = HttpProtocols.Http1AndHttp2)
        where TControl : Control
    {
        return await StartAsync(browser, async control =>
        {
            var innerControl = stateFactory();
            await control.Controls.AddAsync(innerControl);
            return innerControl;
        }, configure, configureApp, headless, enableViewState, protocols);
    }

    public async Task<CurrentState<TState>> StartAsync<TState>(
        Browser browser,
        Func<Control, TState> stateFactory,
        Action<IServiceCollection>? configure = null,
        Action<IApplicationBuilder>? configureApp = null,
        bool? headless = null,
        bool enableViewState = true,
        HttpProtocols protocols = HttpProtocols.Http1AndHttp2)
    {
        return await StartAsync(browser, control => Task.FromResult(stateFactory(control)), configure, configureApp, headless, enableViewState, protocols);
    }

    public async Task<CurrentState<TState>> StartAsync<TState>(
        Browser browser,
        Func<Control, Task<TState>> stateFactory,
        Action<IServiceCollection>? configure = null,
        Action<IApplicationBuilder>? configureApp = null,
        bool? headless = null,
        bool enableViewState = true,
        HttpProtocols protocols = HttpProtocols.Http1AndHttp2)
    {
        var state = new CurrentState<TState>();
        var testContext = await StartAsync<TestControl<TState>>(browser, services =>
        {
            services.AddSingleton<IControlFactory<TestControl<TState>>>(new FuncControlFactory<TestControl<TState>>(_ => new TestControl<TState>(stateFactory, state)));
            configure?.Invoke(services);
        }, configureApp, headless, enableViewState, protocols);

        state.Browser = testContext;

        return state;
    }

    public Task<ITestContext<TControl>> StartAsync<TControl>(
        Browser browser,
        Action<IServiceCollection>? configure = null,
        Action<IApplicationBuilder>? configureApp = null,
        bool? headless = null,
        bool enableViewState = true,
        HttpProtocols protocols = HttpProtocols.Http1AndHttp2)
        where TControl : Control
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
    ) where TControl : Control
    {
        return await StartBrowserAsync<TControl>(_chromeDrivers, DriverFactory, configure, configureApp, enableViewState, protocols);

        IWebDriver DriverFactory()
        {
            var chromeOptions = new ChromeOptions();
            if (headless) chromeOptions.AddArgument("--headless");
            chromeOptions.AddArgument("--disable-search-engine-choice-screen");
            chromeOptions.AddArgument("--no-sandbox");
            chromeOptions.AddArgument("--ignore-certificate-errors");

            return new ChromeDriver(chromeOptions);
        }
    }

    public async Task<ITestContext<TControl>> StartFirefoxAsync<TControl>(
        Action<IServiceCollection>? configure = null,
        Action<IApplicationBuilder>? configureApp = null,
        bool headless = true,
        bool enableViewState = true,
        HttpProtocols protocols = HttpProtocols.Http1AndHttp2
    ) where TControl : Control
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
            return new FirefoxDriver(firefoxOptions);
        }
    }

    private Task<ITestContext<TControl>> StartBrowserAsync<TControl>(
        Context context,
        Func<IWebDriver> driverFactory,
        Action<IServiceCollection>? configure = null,
        Action<IApplicationBuilder>? configureApp = null,
        bool enableViewState = true,
        HttpProtocols protocols = HttpProtocols.Http1AndHttp2
    ) where TControl : Control
    {
        return ControlTest.StartBrowserAsync(CreateDriver, configure, configureApp, enableViewState, protocols);

        async Task<WebServerContext<TControl>> CreateDriver(IHost host)
        {
            await context.Semaphore.WaitAsync(TimeSpan.FromSeconds(30));

            if (!context.Drivers.TryTake(out var driver))
            {
                driver = await CreateDriverWithRetryAsync(driverFactory);
            }

            return new SeleniumTestContext<TControl>(host, driver, context);
        }
    }

    private static async Task<IWebDriver> CreateDriverWithRetryAsync(Func<IWebDriver> driverFactory, int maxRetries = 3)
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return driverFactory();
            }
            catch (WebDriverException ex) when (ex.Message.Contains("Cannot start the driver service"))
            {
                lastException = ex;
                // Wait before retrying, with exponential backoff
                await Task.Delay(TimeSpan.FromMilliseconds(500 * (attempt + 1)));
            }
        }

        throw new WebDriverException($"Failed to start driver after {maxRetries} attempts", lastException);
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