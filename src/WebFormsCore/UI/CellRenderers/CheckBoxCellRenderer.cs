using System.Reflection;
using System.Threading.Tasks;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.CellRenderers;

public class CheckBoxCellRenderer : IGridCellRenderer
{
    public bool SupportsType(PropertyInfo property)
    {
        return property.PropertyType == typeof(bool);
    }

    public async ValueTask CellCreated(PropertyInfo property, TableCell cell, GridItem item)
    {
        var checkBox = cell.Page.WebActivator.CreateControl<CheckBox>();
        checkBox.ID = "cb";
        checkBox.Enabled = false;
        await cell.Controls.AddAsync(checkBox);
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
