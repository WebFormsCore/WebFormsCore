using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using WebFormsCore.UI;

namespace WebFormsCore;

public static class SeleniumExtensions
{
    public static async ValueTask WaitForPageBackAsync(this IWebDriver driver, CancellationToken token = default)
    {
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
            throw new WebDriverTimeoutException("Page did not load in time");
        }
    }

    public static ValueTask ClickAndWaitForPageBackAsync(this IWebElement element, CancellationToken token = default)
    {
        element.Click();

        return element is IWrapsDriver wrapsDriver
            ? wrapsDriver.WrappedDriver.WaitForPageBackAsync(token)
            : default;
    }

    public static IWebElement FindElement(this IWebDriver element, Control control)
    {
        return element.FindElement(By.Id(control.ClientID));
    }
}
