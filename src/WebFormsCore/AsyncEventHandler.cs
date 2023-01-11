using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace WebFormsCore;

public delegate Task AsyncEventHandler(object? sender, EventArgs e);

public delegate Task AsyncEventHandler<in T>(object? sender, T e);

[SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
public static class AsyncEventHandlerHelper
{
    private static readonly Func<MulticastDelegate, (int, object)> GetInvocationList = CreateGetInvocationList();

    private static Func<MulticastDelegate, (int, object)> CreateGetInvocationList()
    {
        var list = typeof(MulticastDelegate).GetField("_invocationList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var count = typeof(MulticastDelegate).GetField("_invocationCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (list == null || count == null)
        {
            return d =>
            {
                var result = d.GetInvocationList();
                return (result.Length, result);
            };
        }

        var parameter = Expression.Parameter(typeof(MulticastDelegate), "d");
        var listAccess = Expression.Field(parameter, list);
        var countAccess = Expression.Field(parameter, count);
        var tuple = Expression.New(typeof(ValueTuple<int, object>).GetConstructor(new[] { typeof(int), typeof(object) })!, Expression.Convert(countAccess, typeof(int)), listAccess);
        var lambda = Expression.Lambda<Func<MulticastDelegate, (int, object)>>(tuple, parameter);
        return lambda.Compile();
    }

    public static async ValueTask InvokeAsync(this AsyncEventHandler? handler, object? sender, EventArgs e)
    {
        if (handler == null) return;

        var (length, result) = GetInvocationList(handler);

        if (result is not object[] objects)
        {
            var task = handler(sender, e);
            if (task != null) await task;
            return;
        }

        for (var index = 0; index < length; index++)
        {
            var @delegate = objects[index];
            var task = Unsafe.As<AsyncEventHandler>(@delegate)(sender, e);
            if (task != null) await task;
        }
    }

    public static async ValueTask InvokeAsync<T>(this AsyncEventHandler<T>? handler, object? sender, T e)
    {
        if (handler == null) return;

        var (length, result) = GetInvocationList(handler);

        if (result is not object[] objects)
        {
            var task = handler(sender, e);
            if (task != null) await task;
            return;
        }

        for (var index = 0; index < length; index++)
        {
            var @delegate = objects[index];
            var task = Unsafe.As<AsyncEventHandler<T>>(@delegate)(sender, e);
            if (task != null) await task;
        }
    }
}
