using System.Diagnostics.CodeAnalysis;
using WebFormsCore.UI.Attributes;

namespace WebFormsCore.UI.WebControls;

[ParseChildren(true)]
[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
public partial class Grid : RepeaterBase<GridItem>, IAttributeAccessor, IDisposable
{
    [ViewState] private AttributeCollection _attributes = new();

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties), ViewState(WriteAlways = true)] private Type? _itemType;
    [ViewState(WriteAlways = true)] private int _itemCount;

    [ViewState] public AttributeCollection RowAttributes { get; set; } = new();
    [ViewState] public AttributeCollection EditRowAttributes { get; set; } = new();

    public ITemplate? EditItemTemplate { get; set; }

    public event AsyncEventHandler<Grid, NeedDataSourceEventArgs>? NeedDataSource;

    public event AsyncEventHandler<Grid, GridItemEventArgs>? ItemCreated;

    public event AsyncEventHandler<Grid, GridItemEventArgs>? ItemDataBound;

    protected override void InitializeItem(GridItem item)
    {

    }

    protected override ValueTask<GridItem?> CreateItemAsync(int itemIndex, ListItemType itemType)
    {
        return itemType is ListItemType.Item or ListItemType.AlternatingItem
            ? new ValueTask<GridItem?>(new GridItem(itemIndex, this))
            : default;
    }

    protected override void SetDataItem(GridItem item, object dataItem)
    {
        item.DataItem = dataItem;
    }

    protected override ValueTask InvokeNeedDataSource(bool filterByKeys)
    {
        return NeedDataSource.InvokeAsync(this, new NeedDataSourceEventArgs(this, filterByKeys));
    }

    protected override async ValueTask InvokeItemDataBound(GridItem item)
    {
        foreach (var cell in item.Cells)
        {
            await cell.Column.InvokeDataBinding(cell, item, Page.IsPostBack);
        }

        await ItemDataBound.InvokeAsync(this, new GridItemEventArgs(item, Page.IsPostBack));
    }

    protected override async ValueTask InvokeItemCreated(GridItem item)
    {
        foreach (var column in Columns)
        {
            var cell = column.CreateCell(Page, item);
            cell.Column = column;
            cell.Grid = this;

            await item.AddCell(cell);
            await column.InvokeItemCreated(cell, item, Page.IsPostBack);
        }

        await item.LoadEditItemTemplateAsync();
        await ItemCreated.InvokeAsync(this, new GridItemEventArgs(item, Page.IsPostBack));
    }

    public List<GridColumn> Columns { get; } = new();

    public int GetColumIndex(GridColumn column) => Columns.IndexOf(column);

    public GridColumn? GetColumn(string id)
    {
        foreach (var currentColumn in Columns)
        {
            if (id.Equals(currentColumn.UniqueName, StringComparison.OrdinalIgnoreCase))
            {
                return currentColumn;
            }
        }

        return null;
    }

    public override async ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        _attributes.AddAttributes(writer);
        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Table);

        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Thead);
        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Tr);

        foreach (var column in Columns)
        {
            await column.RenderAsync(writer, token);
        }

        await writer.RenderEndTagAsync();
        await writer.RenderEndTagAsync();

        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Tbody);

        foreach (var item in Items)
        {
            await item.RenderAsync(writer, token);
        }

        await writer.RenderEndTagAsync();

        await writer.RenderEndTagAsync();
    }

    protected override async ValueTask BeforeDataBindAsync()
    {
        foreach (var column in Columns)
        {
            await Controls.AddAsync(column);
        }
    }

    protected virtual string? GetAttribute(string name) => _attributes[name];

    protected virtual void SetAttribute(string name, string? value) => _attributes[name] = value;

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Keys.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    string? IAttributeAccessor.GetAttribute(string name) => GetAttribute(name);

    /// <inheritdoc />
    void IAttributeAccessor.SetAttribute(string name, string? value) => SetAttribute(name, value);
}
