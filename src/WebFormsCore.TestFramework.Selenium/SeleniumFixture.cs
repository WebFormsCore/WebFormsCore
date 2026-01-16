using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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

    public ISeleniumTestContext Browser { get; set; } = null!;

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
    private readonly BrowserPool _chromePool = new();
    private readonly BrowserPool _firefoxPool = new();

    public async Task<CurrentState<TControl>> StartAsync<TControl>(
        Browser browser,
        Func<TControl> stateFactory,
        SeleniumFixtureOptions? options = null)
        where TControl : Control
    {
        return await StartAsync(browser, async control =>
        {
            var innerControl = stateFactory();
            await control.Controls.AddAsync(innerControl);
            return innerControl;
        }, options);
    }

    public async Task<CurrentState<TState>> StartAsync<TState>(
        Browser browser,
        Func<Control, TState> stateFactory,
        SeleniumFixtureOptions? options = null)
    {
        return await StartAsync(browser, control => Task.FromResult(stateFactory(control)), options);
    }

    public async Task<CurrentState<TState>> StartAsync<TState>(
        Browser browser,
        Func<Control, Task<TState>> stateFactory,
        SeleniumFixtureOptions? options = null)
    {
        options ??= new SeleniumFixtureOptions();
        var state = new CurrentState<TState>();
        var originalConfigure = options.Configure;

        options.Configure = services =>
        {
            services.AddSingleton<IControlFactory<TestControl<TState>>>(new FuncControlFactory<TestControl<TState>>(_ => new TestControl<TState>(stateFactory, state)));
            originalConfigure?.Invoke(services);
        };

        var testContext = await StartAsync<TestControl<TState>>(browser, options);

        state.Browser = (ISeleniumTestContext)testContext;

        return state;
    }

    public Task<ITestContext<TControl>> StartAsync<TControl>(
        Browser browser,
        SeleniumFixtureOptions? options = null)
        where TControl : Control
    {
        options ??= new SeleniumFixtureOptions();
        var headless = options.Headless ?? !Debugger.IsAttached;

        return browser switch
        {
            Browser.Chrome => StartChromeAsync<TControl>(headless, options),
            Browser.Firefox => StartFirefoxAsync<TControl>(headless, options),
            _ => throw new NotSupportedException(),
        };
    }

    public async Task<ITestContext<TControl>> StartChromeAsync<TControl>(
        bool headless,
        SeleniumFixtureOptions options
    ) where TControl : Control
    {
        return await StartBrowserAsync<TControl>(_chromePool, () => CreateChromeDriver(headless), options);
    }

    private static IWebDriver CreateChromeDriver(bool headless)
    {
        var chromeOptions = new ChromeOptions();
        if (headless) chromeOptions.AddArgument("--headless");
        chromeOptions.AddArgument("--disable-search-engine-choice-screen");
        chromeOptions.AddArgument("--no-sandbox");
        chromeOptions.AddArgument("--ignore-certificate-errors");
        chromeOptions.AddArgument("--disable-gpu");
        chromeOptions.AddArgument("--disable-dev-shm-usage");

        return new ChromeDriver(chromeOptions);
    }

    public async Task<ITestContext<TControl>> StartFirefoxAsync<TControl>(
        bool headless,
        SeleniumFixtureOptions options
    ) where TControl : Control
    {
        return await StartBrowserAsync<TControl>(_firefoxPool, () => CreateFirefoxDriver(headless), options);
    }

    private static IWebDriver CreateFirefoxDriver(bool headless)
    {
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

    private Task<ITestContext<TControl>> StartBrowserAsync<TControl>(
        BrowserPool pool,
        Func<IWebDriver> driverFactory,
        SeleniumFixtureOptions options
    ) where TControl : Control
    {
        return ControlTest.StartBrowserAsync(CreateContext, options);

        async Task<WebServerContext<TControl>> CreateContext(IHost host)
        {
            var (driver, tabHandle) = await pool.AcquireTabAsync(driverFactory);
            return new SeleniumTestContext<TControl>(host, driver, tabHandle, pool);
        }
    }

    public void Dispose()
    {
        _chromePool.Dispose();
        _firefoxPool.Dispose();
    }

    internal class BrowserPool : IDisposable
    {
        private const int MaxRetries = 3;
        private const int BaseDelayMs = 500;

        private IWebDriver? _driver;
        private string? _anchorHandle;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _disposed;

        public async Task<(IWebDriver Driver, string TabHandle)> AcquireTabAsync(Func<IWebDriver> driverFactory)
        {
            await _semaphore.WaitAsync();

            if (_disposed)
            {
                _semaphore.Release();
                throw new ObjectDisposedException(nameof(BrowserPool));
            }

            try
            {
                if (_driver is null)
                {
                    _driver = await CreateDriverWithRetryAsync(driverFactory);
                    _anchorHandle = _driver.CurrentWindowHandle;
                }

                // Open a new tab for this test
                _driver.SwitchTo().NewWindow(WindowType.Tab);

                return (_driver, _driver.CurrentWindowHandle);
            }
            catch
            {
                _semaphore.Release();
                throw;
            }
        }

        private static async Task<IWebDriver> CreateDriverWithRetryAsync(Func<IWebDriver> driverFactory)
        {
            Exception? lastException = null;

            for (var attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    return driverFactory();
                }
                catch (WebDriverException ex) when (ex.Message.Contains("Cannot start the driver service"))
                {
                    lastException = ex;

                    // Exponential backoff before retry
                    var delay = BaseDelayMs * (1 << attempt);
                    await Task.Delay(delay);
                }
            }

            throw lastException!;
        }

        public void ReleaseTab(IWebDriver driver, string tabHandle)
        {
            try
            {
                driver.SwitchTo().Window(tabHandle);
                driver.Close();

                driver.SwitchTo().Window(_anchorHandle!);
            }
            catch
            {
                // If something goes wrong, recreate the browser on next acquire
                try { _driver?.Quit(); } catch { /* ignore */ }
                _driver = null;
                _anchorHandle = null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _semaphore.Wait();
            _disposed = true;
            try { _driver?.Quit(); } catch { /* ignore */ }
            _driver = null;
            _semaphore.Release();
        }
    }
}