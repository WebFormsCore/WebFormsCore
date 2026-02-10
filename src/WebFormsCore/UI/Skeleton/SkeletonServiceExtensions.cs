using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebFormsCore.UI;
using WebFormsCore.UI.Skeleton;
using WebFormsCore.UI.Skeleton.Renderers;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public static class SkeletonServiceExtensions
{
    /// <summary>
    /// Adds skeleton rendering support to WebFormsCore.
    /// Registers the default renderers for common control types and a generic fallback.
    /// </summary>
    public static IWebFormsCoreBuilder AddSkeletonSupport(this IWebFormsCoreBuilder builder)
    {
        builder.Services.TryAddSingleton(typeof(ISkeletonRenderer<>), typeof(SkeletonRenderer<>));

        builder.AddSkeletonRenderer<WebControl, WebControlSkeletonRenderer>();
        builder.AddSkeletonRenderer<HtmlContainerControl, HtmlContainerControlSkeletonRenderer>();
        builder.AddSkeletonRenderer<Label, LabelSkeletonRenderer>();
        builder.AddSkeletonRenderer<TextBox, TextBoxSkeletonRenderer>();
        builder.AddSkeletonRenderer<Button, ButtonSkeletonRenderer>();

        return builder;
    }

    /// <summary>
    /// Registers a custom skeleton renderer for a specific control type.
    /// </summary>
    public static IWebFormsCoreBuilder AddSkeletonRenderer<TControl, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRenderer>(this IWebFormsCoreBuilder builder)
        where TControl : Control
        where TRenderer : class, ISkeletonRenderer<TControl>
    {
        builder.Services.TryAddSingleton<TRenderer>();
        builder.Services.TryAddSingleton<ISkeletonRenderer<TControl>>(p => p.GetRequiredService<TRenderer>());

        return builder;
    }
}
