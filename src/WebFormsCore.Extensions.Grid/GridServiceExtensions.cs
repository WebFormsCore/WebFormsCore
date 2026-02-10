using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.CellRenderers;
using WebFormsCore.UI.Skeleton.Renderers;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public static class GridServiceExtensions
{
    public static IWebFormsCoreBuilder AddGridCellRenderers(this IWebFormsCoreBuilder builder)
    {
        builder.Services.AddSingleton<IGridCellRenderer, CheckBoxCellRenderer>();
        return builder;
    }

    /// <summary>
    /// Registers the <see cref="GridSkeletonRenderer"/> so that <see cref="Grid"/> controls
    /// render skeleton table rows when inside a skeleton container or lazy loader.
    /// Requires <see cref="SkeletonServiceExtensions.AddSkeletonSupport"/> to be called first.
    /// </summary>
    public static IWebFormsCoreBuilder AddGridSkeletonSupport(this IWebFormsCoreBuilder builder)
    {
        builder.AddSkeletonRenderer<Grid, GridSkeletonRenderer>();
        return builder;
    }
}
