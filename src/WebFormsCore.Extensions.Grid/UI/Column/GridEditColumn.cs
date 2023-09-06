using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI;

public class GridEditColumn : GridColumn
{
    public override GridCell CreateCell(Page page, GridItem item)
    {
        var cell = base.CreateCell(page, item);

        var button = new Button();

        cell.Controls.AddWithoutPageEvents(button);

        button.Text = "Edit";

        button.Click += static (sender, args) =>
        {
            var button = (Button)sender!;
            var item = button.FindParent<GridItem>();
            return item?.ToggleEditAsync() ?? Task.CompletedTask;
        };

        return cell;
    }
}
