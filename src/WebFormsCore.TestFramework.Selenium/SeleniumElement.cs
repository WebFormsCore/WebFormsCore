using System;
using System.Threading.Tasks;
using OpenQA.Selenium;

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