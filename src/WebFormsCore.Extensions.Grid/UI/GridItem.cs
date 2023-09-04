using System.Reflection;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI;

public partial class TableCell : WebControl
{
    public TableCell()
        : base(HtmlTextWriterTag.Td)
    {
    }

    protected override bool GenerateAutomaticID => false;

    [ViewState] public string? Text { get; set; }

    public override void ClearControl()
    {
        base.ClearControl();
        Text = null;
    }

    protected override Task RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return HasRenderingData()
            ? base.RenderContentsAsync(writer, token)
            : writer.WriteAsync(Text);
    }
}

public class TableRow : WebControl
{
    public TableRow()
        : base(HtmlTextWriterTag.Tr)
    {
    }

    protected override bool GenerateAutomaticID => false;
}

public class GridCell : TableCell
{
    public Grid Grid { get; set; } = null!;

    public GridColumn Column { get; set; } = null!;

    public override bool Visible
    {
        get => Column.Visible;
        set => throw new NotSupportedException();
    }

    public IGridCellRenderer? Renderer { get; set; }

    public PropertyInfo? PropertyInfo { get; set; }

    public override void ClearControl()
    {
        base.ClearControl();

        Grid = null!;
        Column = null!;
    }

    public override async Task RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        await RenderBeginTag(writer, token);

        if (Visible)
        {
            await RenderContentsAsync(writer, token);
        }

        await RenderEndTagAsync(writer, token);
    }
}

public class GridItem : TableRow, IDataItemContainer
{
    internal readonly List<GridCell> Cells = new();
    private object? _dataItem;

    public override string UniqueID => Parent.UniqueID + IdSeparator + base.UniqueID;

    public override string ClientID => Parent.ClientID + '_' + base.ClientID;

    public GridItem(int itemIndex, Grid grid)
    {
        ItemIndex = itemIndex;
        Grid = grid;
    }

    public Grid Grid { get; }

    protected virtual object? GetDataItem() => _dataItem;

    protected virtual void SetDataItem(object? value) => _dataItem = value;

    public virtual object? DataItem
    {
        get => GetDataItem();
        set => SetDataItem(value);
    }

    public TableCell this[int index] => Cells[index];

    public TableCell this[string name]
    {
        get
        {
            var item = Grid.GetColumn(name);

            if (item == null)
            {
                throw new ArgumentOutOfRangeException(nameof(name), $"Column '{name}' not found.");
            }

            var index = Grid.GetColumIndex(item);

            return Cells[index];
        }
    }

    public virtual int ItemIndex { get; set; }

    public virtual ListItemType ItemType { get; set; }

    public virtual Task DataBindAsync()
    {
        return Task.CompletedTask;
    }

    int IDataItemContainer.DataItemIndex => ItemIndex;

    int IDataItemContainer.DisplayIndex => ItemIndex;

    internal ValueTask AddCell(GridCell cell)
    {
        Cells.Add(cell);
        return Controls.AddAsync(cell);
    }
}
