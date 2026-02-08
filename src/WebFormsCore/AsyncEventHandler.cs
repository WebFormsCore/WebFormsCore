using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace WebFormsCore;

public delegate Task AsyncEventHandler(object sender, EventArgs e);

public delegate Task AsyncEventHandler<in TSender, in TArgs>(TSender sender, TArgs e);

[SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
public static class AsyncEventHandlerHelper
{
    public static async ValueTask InvokeAsync<TSender, TArgs>(this AsyncEventHandler<TSender, TArgs>? handler, TSender sender, TArgs e)
        where TSender : class
    {
        if (handler == null) return;

        foreach (var @delegate in Delegate.EnumerateInvocationList(handler))
        {
            var task = @delegate(sender, e);
            if (task != null) await task;
        }
    }

    public static async ValueTask InvokeAsync(this AsyncEventHandler? handler, object sender, EventArgs e)
    {
        if (handler == null) return;

        foreach (var @delegate in Delegate.EnumerateInvocationList(handler))
        {
            var task = @delegate(sender, e);
            if (task != null) await task;
        }
    }
}
