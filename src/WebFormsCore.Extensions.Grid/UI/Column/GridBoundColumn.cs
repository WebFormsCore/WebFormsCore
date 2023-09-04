using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore.UI;

public class GridBoundColumn : GridColumn
{
    public string? DataField { get; set; }

    public override async ValueTask InvokeItemCreated(GridCell cell, GridItem item)
    {
        var member = DataField is null ? null : item.Grid.ItemType?.GetProperty(DataField);

        if (member != null)
        {
            cell.PropertyInfo = member;

            var renderers = Context.RequestServices.GetServices<IGridCellRenderer>();

            foreach (var renderer in renderers)
            {
                if (renderer.SupportsType(member))
                {
                    await renderer.CellCreated(member!, cell, item);
                    cell.Renderer = renderer;
                }
            }
        }

        await base.InvokeItemCreated(cell, item);
    }

    public override async ValueTask InvokeDataBinding(GridCell cell, GridItem item)
    {
        if (DataField is not null)
        {
            var value = cell.PropertyInfo?.GetValue(item.DataItem);

            if (cell is { Renderer: not null, PropertyInfo: not null })
            {
                await cell.Renderer.CellDataBinding(cell.PropertyInfo, cell, item, value);
            }
            else
            {
                cell.Text = value?.ToString();
            }
        }

        await base.InvokeDataBinding(cell, item);
    }

    protected override string? GetDefaultUniqueName()
    {
        return DataField;
    }
}