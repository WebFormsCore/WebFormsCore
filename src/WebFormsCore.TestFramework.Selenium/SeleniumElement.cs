using System;
using System.Text.Json;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;

namespace WebFormsCore;

internal class SeleniumElement(IWebElement element) : IElement
{
    public string Text => element.Text;

    public string Value
    {
        get
        {
            switch (element.TagName)
            {
                case "input":
                    return element.GetAttribute("value");
                case "textarea":
                    return element.Text;
                default:
                    throw new NotSupportedException($"Element tag name '{element.TagName}' is not supported.");
            }
        }
        set
        {
            switch (element.TagName)
            {
                case "input":
                case "textarea":
                    element.Clear();
                    element.SendKeys(value);
                    break;
                default:
                    throw new NotSupportedException($"Element tag name '{element.TagName}' is not supported.");
            }
        }
    }

    public ValueTask ClearAsync()
    {
        element.Clear();
        return default;
    }

    public ValueTask ClickAsync()
    {
        return element.ClickAndWaitForPageBackAsync();
    }

    public ValueTask SelectAsync(string value)
    {
        var dropDown = new SelectElement(element);
        dropDown.SelectByValue(value);
        return element.WaitForPostBack();
    }

    public async ValueTask PostBackAsync(string? argument = null, PostBackOptions? options = null)
    {
        if (element is IWrapsDriver wrapsDriver)
        {
            if (options == null)
            {
                wrapsDriver.WrappedDriver.ExecuteJavaScript(
                    "wfc.postBack(document.getElementById(arguments[0].id), arguments[1])",
                    element,
                    argument);
            }
            else
            {
                wrapsDriver.WrappedDriver.ExecuteJavaScript(
                    "wfc.postBack(document.getElementById(arguments[0].id), arguments[1], JSON.parse(arguments[2]))",
                    element,
                    argument,
                    JsonSerializer.Serialize(options));
            }

            await wrapsDriver.WrappedDriver.WaitForPageBackAsync();
        }
    }

    public ValueTask TypeAsync(string text)
    {
        element.SendKeys(text);
        return default;
    }

    public ValueTask<bool> IsVisibleAsync()
    {
        return new ValueTask<bool>(element.Displayed);
    }

    public ValueTask<string> GetAttributeAsync(string name)
    {
        return new ValueTask<string>(element.GetAttribute(name));
    }
}