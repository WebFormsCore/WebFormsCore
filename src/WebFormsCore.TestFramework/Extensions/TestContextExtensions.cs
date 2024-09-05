using System;
using WebFormsCore.UI;

namespace WebFormsCore;

public static class TestContextExtensions
{
    public static IElement? GetElement(this ITestContext context, Control control)
    {
        var id = control.ClientID ?? throw new InvalidOperationException("ClientID is not available");

        return context.GetElementById(id);
    }

    public static IElement GetRequiredElement(this ITestContext context, Control control)
    {
        return context.GetElement(control) ?? throw new InvalidOperationException("Element not found");
    }

    public static IElement GetRequiredElementById(this ITestContext context, string id)
    {
        return context.GetElementById(id) ?? throw new InvalidOperationException("Element not found");
    }

    public static IElement QuerySelectorRequired(this ITestContext context, string selector)
    {
        return context.QuerySelector(selector) ?? throw new InvalidOperationException("Element not found");
    }
}