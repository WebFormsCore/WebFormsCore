using OpenQA.Selenium;

namespace WebFormsCore;

/// <summary>
/// Provides access to the Selenium WebDriver for test contexts.
/// </summary>
public interface ISeleniumTestContext : ITestContext
{
    /// <summary>
    /// Gets the underlying Selenium WebDriver.
    /// </summary>
    IWebDriver Driver { get; }
}
