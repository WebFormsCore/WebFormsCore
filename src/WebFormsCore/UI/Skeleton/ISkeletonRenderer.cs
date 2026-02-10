using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI.Skeleton;

/// <summary>
/// Non-generic base interface for skeleton rendering.
/// Enables resolving renderers via IServiceProvider when the
/// concrete control type is not known at compile time.
/// </summary>
public interface ISkeletonRenderer
{
    /// <summary>
    /// Renders a skeleton placeholder for the given control.
    /// </summary>
    ValueTask RenderSkeletonAsync(Control control, HtmlTextWriter writer, CancellationToken token);
}

/// <summary>
/// Defines how to render a skeleton placeholder for a specific control type.
/// Implementations should render an HTML element that visually resembles the
/// shape of the actual control but with placeholder styling.
/// </summary>
/// <typeparam name="TControl">The control type this renderer handles.</typeparam>
public interface ISkeletonRenderer<in TControl> : ISkeletonRenderer
    where TControl : Control
{
    /// <summary>
    /// Renders a skeleton placeholder for the given control.
    /// </summary>
    ValueTask RenderSkeletonAsync(TControl control, HtmlTextWriter writer, CancellationToken token);

    /// <inheritdoc />
    ValueTask ISkeletonRenderer.RenderSkeletonAsync(Control control, HtmlTextWriter writer, CancellationToken token)
    {
        return control is TControl typedControl
            ? RenderSkeletonAsync(typedControl, writer, token)
            : default;
    }
}
