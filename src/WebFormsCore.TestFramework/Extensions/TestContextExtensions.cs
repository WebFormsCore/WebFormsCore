using System;
using WebFormsCore.UI;

namespace WebFormsCore;

public static class TestContextExtensions
{
    public static IElement FindElement(this ITestContext context, Control control)
    {
        var id = control.ClientID ?? throw new InvalidOperationException("ClientID is not available");

        return context.GetElementById(id);
    }
}