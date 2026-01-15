using System;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using WebFormsCore.UI;

namespace WebFormsCore;

public static class SeleniumExtensions
{
    /// <summary>
    /// Waits until the StreamPanel's WebSocket is connected.
    /// </summary>
    /// <param name="driver">The web driver.</param>
    /// <param name="selector">CSS selector for the StreamPanel element (default: [data-wfc-stream]).</param>
    /// <param name="timeout">Maximum time to wait for the connection.</param>
    /// <exception cref="WebDriverTimeoutException">Thrown if the WebSocket does not connect within the timeout.</exception>
    public static void WaitForStreamPanelConnection(
        this IWebDriver driver,
        string selector = "[data-wfc-stream]",
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        var wait = new WebDriverWait(driver, timeout.Value);

        wait.Until(d =>
        {
            var js = (IJavaScriptExecutor)d;
            var result = js.ExecuteScript($@"
                var el = document.querySelector('{selector}');
                if (!el || !el.webSocket) return false;
                return el.webSocket.readyState === 1;
            ");
            return result is true or "true";
        });
    }

    /// <summary>
    /// Waits until the StreamPanel's WebSocket connection fails or closes.
    /// </summary>
    /// <param name="driver">The web driver.</param>
    /// <param name="selector">CSS selector for the StreamPanel element (default: [data-wfc-stream]).</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <exception cref="WebDriverTimeoutException">Thrown if the timeout is reached.</exception>
    public static void WaitForStreamPanelDisconnection(
        this IWebDriver driver,
        string selector = "[data-wfc-stream]",
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        var wait = new WebDriverWait(driver, timeout.Value);

        wait.Until(d =>
        {
            var js = (IJavaScriptExecutor)d;
            var result = js.ExecuteScript($@"
                var el = document.querySelector('{selector}');
                if (!el || !el.webSocket) return true;
                return el.webSocket.readyState >= 2;
            ");
            return result is true or "true";
        });
    }

    public static async ValueTask WaitForPageBackAsync(this IWebDriver driver, CancellationToken token = default)
    {
        await Task.Delay(10, token);

        if (driver is not IJavaScriptExecutor js)
        {
            return;
        }

        for (var i = 0; i < 60; i++)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            var readyState = js.ExecuteScript("return 'wfc' in window && !wfc.hasPendingPostbacks");

            if (readyState is "true" or true)
            {
                return;
            }

            await Task.Delay(50, token);
        }

        if (!token.IsCancellationRequested)
        {
            var wfcLoaded = js.ExecuteScript("return 'wfc' in window");
            var hasPendingPostbacks = js.ExecuteScript("return wfc.hasPendingPostbacks");

            throw new WebDriverTimeoutException($"Page did not load in time (wfc: {wfcLoaded}, hasPendingPostbacks: {hasPendingPostbacks})");
        }
    }

    public static ValueTask ClickAndWaitForPageBackAsync(this IWebElement element, CancellationToken token = default)
    {
        element.Click();

        return WaitForPostBack(element, token);
    }

    internal static ValueTask WaitForPostBack(this IWebElement element, CancellationToken token = default)
    {
        return element is IWrapsDriver wrapsDriver
            ? wrapsDriver.WrappedDriver.WaitForPageBackAsync(token)
            : default;
    }

    public static IWebElement FindElement(this IWebDriver element, Control control)
    {
        return element.FindElement(By.Id(control.ClientID));
    }

    /// <summary>
    /// Waits until the StreamPanel's WebSocket is connected.
    /// </summary>
    /// <param name="context">The Selenium test context.</param>
    /// <param name="selector">CSS selector for the StreamPanel element (default: [data-wfc-stream]).</param>
    /// <param name="timeout">Maximum time to wait for the connection.</param>
    /// <exception cref="WebDriverTimeoutException">Thrown if the WebSocket does not connect within the timeout.</exception>
    public static void WaitForStreamPanelConnection(
        this ISeleniumTestContext context,
        string selector = "[data-wfc-stream]",
        TimeSpan? timeout = null)
    {
        context.Driver.WaitForStreamPanelConnection(selector, timeout);
    }

    /// <summary>
    /// Waits until the StreamPanel's WebSocket connection fails or closes.
    /// </summary>
    /// <param name="context">The Selenium test context.</param>
    /// <param name="selector">CSS selector for the StreamPanel element (default: [data-wfc-stream]).</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <exception cref="WebDriverTimeoutException">Thrown if the timeout is reached.</exception>
    public static void WaitForStreamPanelDisconnection(
        this ISeleniumTestContext context,
        string selector = "[data-wfc-stream]",
        TimeSpan? timeout = null)
    {
        context.Driver.WaitForStreamPanelDisconnection(selector, timeout);
    }
}
