using System.Reflection;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public class CheckBoxCellRenderer : IGridCellRenderer
{
    public bool SupportsType(PropertyInfo property)
    {
        return property.PropertyType == typeof(bool);
    }

    public async ValueTask CellCreated(PropertyInfo property, TableCell cell, GridItem item)
    {
        await cell.Controls.AddAsync(new CheckBox
        {
            ID = "cb",
            Enabled = false
        });
    }

    public ValueTask CellDataBinding(PropertyInfo property, TableCell cell, GridItem item, object? value)
    {
        var cb = (CheckBox?)cell.FindControl("cb");

        if (cb != null)
        {
            cb.Checked = (bool)value!;
        }

        return default;
    }
}
