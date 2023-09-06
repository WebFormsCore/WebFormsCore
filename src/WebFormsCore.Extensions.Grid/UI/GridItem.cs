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

public class GridCell : TableCell, INamingContainer
{
    public override string UniqueID => $"{Parent.UniqueID}{IdSeparator}td{ColumnIndex}";

    public override string ClientID => $"{Parent.ClientID}_td{ColumnIndex}";

    public int ColumnIndex { get; set; }

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

public partial class GridItem : TableRow, IDataItemContainer, IPostBackLoadHandler
{
    [ViewState] private bool _showEdit;
    internal readonly List<GridCell> Cells = new();
    private EditContainer? _editItemTemplateContainer;
    private object? _dataItem;

    public override string UniqueID => $"{Parent.UniqueID}{IdSeparator}tr{ItemIndex}";

    public override string ClientID => $"{Parent.ClientID}_tr{ItemIndex}";

    public GridItem(int itemIndex, Grid grid)
    {
        ItemIndex = itemIndex;
        Grid = grid;
    }

    [ViewState] public bool Selected { get; set; }

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

    public async Task ShowEditAsync()
    {
        _showEdit = true;
        await InitializeEditAsync();
    }

    public async Task ToggleEditAsync()
    {
        if (_showEdit)
        {
            CloseEdit();
        }
        else
        {
            await ShowEditAsync();
        }
    }

    public void CloseEdit()
    {
        _showEdit = false;

        if (_editItemTemplateContainer is null) return;

        Controls.Remove(_editItemTemplateContainer);
        _editItemTemplateContainer = null;
    }

    private async Task InitializeEditAsync()
    {
        if (_editItemTemplateContainer is not null) return;

        _editItemTemplateContainer = new EditContainer(this);
        Grid.EditItemTemplate?.InstantiateIn(_editItemTemplateContainer);
        await Controls.AddAsync(_editItemTemplateContainer);
    }

    async Task IPostBackLoadHandler.AfterPostBackLoadAsync()
    {
        if (_showEdit)
        {
            await InitializeEditAsync();
        }
    }

    private class EditContainer : Control, INamingContainer
    {
        public override string UniqueID => $"{Parent.UniqueID}{IdSeparator}edit";

        public override string ClientID => $"{Parent.ClientID}_edit";

        private readonly GridItem _item;

        public EditContainer(GridItem item)
        {
            _item = item;
        }

        public override async Task RenderAsync(HtmlTextWriter writer, CancellationToken token)
        {
            if (!Visible) return;

            await writer.RenderBeginTagAsync(HtmlTextWriterTag.Tr);

            writer.AddAttribute(HtmlTextWriterAttribute.Colspan, _item.Cells.Count(i => i.Visible).ToString());
            await writer.RenderBeginTagAsync(HtmlTextWriterTag.Td);

            await base.RenderAsync(writer, token);

            await writer.RenderEndTagAsync();
            await writer.RenderEndTagAsync();
        }
    }
}
