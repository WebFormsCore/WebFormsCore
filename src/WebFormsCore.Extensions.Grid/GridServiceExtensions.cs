using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.CellRenderers;

namespace WebFormsCore;

public static class GridServiceExtensions
{
    public static IWebFormsCoreBuilder AddGridCellRenderers(this IWebFormsCoreBuilder builder)
    {
        builder.Services.AddSingleton<IGridCellRenderer, CheckBoxCellRenderer>();
        return builder;
    }
}
