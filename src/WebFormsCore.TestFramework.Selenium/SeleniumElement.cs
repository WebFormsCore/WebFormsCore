﻿using System.Threading.Tasks;
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
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> IsVisibleAsync()
    {
        return ValueTask.FromResult(element.Displayed);
    }

    public ValueTask<string> GetAttributeAsync(string name)
    {
        return ValueTask.FromResult(element.GetAttribute(name));
    }
}