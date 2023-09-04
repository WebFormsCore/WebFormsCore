namespace WebFormsCore.UI;

public class GridTemplateColumn : GridColumn
{
    public ITemplate? ItemTemplate { get; set; }

    public override GridCell CreateCell(Page page, GridItem item)
    {
        var cell = base.CreateCell(page, item);
        ItemTemplate?.InstantiateIn(cell);
        return cell;
    }
}