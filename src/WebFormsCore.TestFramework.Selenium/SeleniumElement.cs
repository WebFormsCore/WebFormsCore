using System.Threading.Tasks;
using OpenQA.Selenium;

namespace WebFormsCore;

internal class SeleniumElement(IWebElement element) : IElement
{
    public string Text => element.Text;

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