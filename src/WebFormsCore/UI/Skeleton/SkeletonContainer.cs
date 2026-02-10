using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.Skeleton;

/// <summary>
/// A container control that, when <see cref="Loading"/> is <c>true</c>, renders skeleton
/// placeholders for its child controls instead of their actual content.
/// When <see cref="Loading"/> is <c>false</c>, renders children normally.
/// </summary>
public partial class SkeletonContainer : WebControl
{
    public SkeletonContainer()
        : base(HtmlTextWriterTag.Div)
    {
    }

    /// <summary>
    /// When <c>true</c>, child controls are rendered as skeleton placeholders.
    /// When <c>false</c>, child controls render normally.
    /// </summary>
    [ViewState]
    public bool Loading { get; set; }

    /// <summary>
    /// Optional CSS class to add to the container when loading.
    /// </summary>
    [ViewState]
    public string? LoadingCssClass { get; set; }

    protected override ValueTask AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        if (Loading)
        {
            if (!string.IsNullOrEmpty(LoadingCssClass))
            {
                writer.MergeAttribute(HtmlTextWriterAttribute.Class, LoadingCssClass);
            }

            writer.AddAttribute("data-wfc-skeleton-container", null);
            writer.AddAttribute("aria-busy", "true");
        }

        return base.AddAttributesToRender(writer, token);
    }

    protected override async ValueTask RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (!Loading)
        {
            await base.RenderContentsAsync(writer, token);
            return;
        }

        if (Controls.Count == 0)
        {
            await RenderGenericSkeleton(writer);
            return;
        }

        await RenderChildSkeletonsAsync(writer, Controls, ServiceProvider, token);
    }

    internal static async ValueTask RenderChildSkeletonsAsync(
        HtmlTextWriter writer,
        ControlCollection controls,
        IServiceProvider serviceProvider,
        CancellationToken token)
    {
        foreach (var control in controls)
        {
            if (token.IsCancellationRequested)
                return;

            // LiteralControl and LiteralHtmlControl render structural HTML as-is
            if (control is LiteralControl or LiteralHtmlControl)
            {
                await control.RenderAsync(writer, token);
                continue;
            }

            if (!control.SelfVisible)
                continue;

            var renderer = ResolveRenderer(serviceProvider, control);

            if (renderer is not null)
            {
                await renderer.RenderSkeletonAsync(control, writer, token);
            }
            else if (control.HasControls())
            {
                // Recurse into children if no renderer found
                await RenderChildSkeletonsAsync(writer, control.Controls, serviceProvider, token);
            }
        }
    }

    private static ISkeletonRenderer? ResolveRenderer(IServiceProvider serviceProvider, Control control)
    {
        var rendererType = typeof(ISkeletonRenderer<>).MakeGenericType(control.GetType());
        return (ISkeletonRenderer?)serviceProvider.GetService(rendererType);
    }

    private static async ValueTask RenderGenericSkeleton(HtmlTextWriter writer)
    {
        writer.AddAttribute(HtmlTextWriterAttribute.Class, "wfc-skeleton");
        writer.AddAttribute("data-wfc-skeleton", null);
        writer.AddAttribute("aria-busy", "true");
        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Div);
        await writer.WriteAsync("&nbsp;");
        await writer.RenderEndTagAsync();
    }

    public override void ClearControl()
    {
        base.ClearControl();

        Loading = false;
        LoadingCssClass = null;
    }
}
