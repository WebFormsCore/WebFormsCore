using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls;

public class GridEditColumn : GridColumn
{
    public ITemplate? ButtonTemplate { get; set; }

    public string ButtonText { get; set; } = "Edit";

    public AttributeCollection ButtonAttributes { get; set; } = new();

    public override GridCell CreateCell(Page page, GridItem item)
    {
        var cell = base.CreateCell(page, item);

        var button = new Button();

        ButtonAttributes.CopyTo(button);

        cell.Controls.AddWithoutPageEvents(button);

        if (ButtonTemplate != null)
        {
            ButtonTemplate.InstantiateIn(button);
        }
        else
        {
            button.Text = ButtonText;
        }

        button.Click += static (sender, args) =>
        {
            var item = sender.FindParent<GridItem>()!;

            item.EditMode = !item.EditMode;
            return Task.CompletedTask;
        };

        return cell;
    }
}
