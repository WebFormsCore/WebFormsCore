using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore.UI.Skeleton;

/// <summary>
/// Generic fallback skeleton renderer that walks the type hierarchy to find
/// a registered renderer for base types.
/// </summary>
/// <typeparam name="T">The control type to render a skeleton for.</typeparam>
internal sealed class SkeletonRenderer<T> : ISkeletonRenderer<T>
    where T : Control
{
    private static readonly ConcurrentDictionary<Type, Type?> Cache = new();

    private readonly IServiceProvider _serviceProvider;

    public SkeletonRenderer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ValueTask RenderSkeletonAsync(T control, HtmlTextWriter writer, CancellationToken token)
    {
        var renderer = Resolve(control);

        return renderer is not null
            ? renderer.RenderSkeletonAsync(control, writer, token)
            : default;
    }

    private ISkeletonRenderer? Resolve(Control control)
    {
        var controlType = control.GetType();
        var rendererInterfaceType = Cache.GetOrAdd(controlType, static (type, sp) => FindRendererType(type, sp), _serviceProvider);

        if (rendererInterfaceType is null)
            return null;

        return (ISkeletonRenderer?)_serviceProvider.GetService(rendererInterfaceType);
    }

    private static Type? FindRendererType(Type controlType, IServiceProvider serviceProvider)
    {
        // Skip the control type itself (we are the fallback for it)
        var current = controlType.BaseType;

        while (current is not null && typeof(Control).IsAssignableFrom(current))
        {
            var rendererType = typeof(ISkeletonRenderer<>).MakeGenericType(current);

            if (serviceProvider.GetService(rendererType) is not null)
                return rendererType;

            current = current.BaseType;
        }

        return null;
    }
}
