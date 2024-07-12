using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Providers;
using WebFormsCore.UI.Attributes;

namespace WebFormsCore.UI.WebControls;

[ParseChildren(true)]
[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
public partial class Grid : WebControl, IPostBackLoadHandler, IDataSourceConsumer, IDisposable, IDataKeyProvider, INeedDataSourceProvider
{
    private readonly List<GridItem> _items = new();
    private bool _ignorePaging;
    private bool _isPostBack;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties), ViewState(WriteAlways = true)] private Type? _itemType;
    [ViewState(WriteAlways = true)] private int _itemCount;
    [ViewState] public KeyCollection Keys { get; set; }

    [ViewState] private int _pageIndex;
    [ViewState] private int? _dataCount;
    [ViewState] public AttributeCollection RowAttributes { get; set; } = new();
    [ViewState] public AttributeCollection EditRowAttributes { get; set; } = new();

    public IReadOnlyList<GridItem> Items => _items;

    private IDataSource? _dataSource;

    public string[] DataKeys { get; set; } = Array.Empty<string>();

    public event AsyncEventHandler<Grid, GridItemEventArgs>? ItemCreated;

    public event AsyncEventHandler<Grid, GridItemEventArgs>? ItemDataBound;

    public event AsyncEventHandler<Grid, NeedDataSourceEventArgs>? NeedDataSource;

    public ITemplate? EditItemTemplate { get; set; }

    public Grid()
        : base(HtmlTextWriterTag.Table)
    {
        Keys = new KeyCollection(this);
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public virtual Type? ItemType => _itemType;

    public object? DataSource
    {
        get => _dataSource?.Value;
        [RequiresDynamicCode("The element type of the data source is not known at compile time. Use SetDataSource<T>(source) instead.")]
        set => _dataSource = value is not null ? new DataSource(value) : null;
    }

    public int? PageCount => PageSize.HasValue && _dataCount.HasValue ? (int)Math.Ceiling((double)_dataCount / PageSize.Value) : null;

    public int? DataCount => _dataCount;

    public List<GridColumn> Columns { get; } = new();

    [ViewState] public virtual bool RenderHeader { get; set; } = true;

    [ViewState] public virtual bool AllowPaging { get; set; } = true;

    [ViewState] public virtual int? PageSize { get; set; }

    public virtual int PageIndex
    {
        get => _pageIndex;
        set
        {
            _pageIndex = value;
            UpdatePaging();
        }
    }

    private void UpdatePaging()
    {
        if (_pageIndex < 0)
        {
            _pageIndex = 0;
        }

        var count = PageCount;

        if (_pageIndex >= count)
        {
            _pageIndex = count.Value - 1;
        }
    }

    public async Task AfterPostBackLoadAsync()
    {
        _isPostBack = true;

        try
        {
            var count = _itemCount;

            if (NeedDataSource != null)
            {
                if (_items.Count != count)
                {
                    var filterByKeys = DataKeys.Length > 0;

                    await NeedDataSource.InvokeAsync(this, new NeedDataSourceEventArgs(this, filterByKeys));
                    _ignorePaging = false;
                }

                if (_items.Count != count)
                {
                    throw new InvalidOperationException($"The number of items in the grid ({_items.Count}) does not match the number of items in the data source ({count}).");
                }

                Keys.Validate();

                return;
            }

            if (count == 0)
            {
                return;
            }

            await ClearAsync();

            _itemCount = count;

            for (var i = 0; i < count; i++)
            {
                await CreateItemAsync(i);
            }
        }
        finally
        {
            _isPostBack = false;
        }
    }

    private async ValueTask ClearAsync()
    {
        _itemCount = 0;
        _items.Clear();
        Controls.Clear();

        foreach (var column in Columns)
        {
            await Controls.AddAsync(column);
        }
    }

    public async ValueTask LoadDataSourceAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(object source)
    {
        await LoadDataSourceCoreAsync<T>(source);
        Keys.Store();
    }

    private ValueTask LoadDataSourceCoreAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(object source)
    {
        return source switch
        {
            IQueryable<T> queryable => LoadDataSourceQueryable(queryable),
            IAsyncEnumerable<T> asyncEnumerable => LoadDataSourceAsyncEnumerable(asyncEnumerable),
            IEnumerable<T> enumerable => LoadDataSourceEnumerable(enumerable),
            _ => throw new InvalidOperationException($"The type {source.GetType().FullName} does not implement IQueryable<T>, IAsyncEnumerable<T> or IEnumerable<T>.")
        };
    }

    public override async ValueTask DataBindAsync(CancellationToken token = default)
    {
        if (_dataSource is null)
        {
            await NeedDataSource.InvokeAsync(this, new NeedDataSourceEventArgs(this, false));
        }

        if (_dataSource is null)
        {
            throw new InvalidOperationException("The DataSource property must be set on the grid.");
        }

        await _dataSource.LoadAsync(this);
        await InvokeDataBindingAsync(token);
    }

    protected async ValueTask LoadDataSourceQueryable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(IQueryable<T> dataSource)
    {
        var queryableProvider = Context.RequestServices.GetRequiredService<IQueryableProvider>();

        _itemType = typeof(T);

        var count = await queryableProvider.CountAsync(dataSource);
        _dataCount = count;

        UpdatePaging();

        if (PageSize.HasValue && !_ignorePaging)
        {
            if (PageIndex > 0)
            {
                dataSource = dataSource.Skip(PageIndex * PageSize.Value);
            }

            dataSource = dataSource.Take(PageSize.Value);
        }

        await ClearAsync();

        var list = await queryableProvider.ToListAsync(dataSource, count);

        foreach (var dataItem in list)
        {
            await CreateAndBindItemAsync(dataItem);
        }
    }

    protected async ValueTask LoadDataSourceEnumerable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(IEnumerable<T> dataSource)
    {
        _itemType = typeof(T);
        _dataCount = dataSource.Count();

        UpdatePaging();

        if (PageSize.HasValue && !_ignorePaging)
        {
            if (PageIndex > 0)
            {
                dataSource = dataSource.Skip(PageIndex * PageSize.Value);
            }

            dataSource = dataSource.Take(PageSize.Value);
        }

        await ClearAsync();

        foreach (var dataItem in dataSource)
        {
            await CreateAndBindItemAsync(dataItem);
        }
    }

    protected async ValueTask LoadDataSourceAsyncEnumerable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(IAsyncEnumerable<T> dataSource)
    {
        _itemType = typeof(T);
        _dataCount = await dataSource.CountAsync();

        UpdatePaging();

        if (PageSize.HasValue && !_ignorePaging)
        {
            if (PageIndex > 0)
            {
                dataSource = dataSource.Skip(PageIndex * PageSize.Value);
            }

            dataSource = dataSource.Take(PageSize.Value);
        }

        await ClearAsync();

        await foreach (var dataItem in dataSource)
        {
            await CreateAndBindItemAsync(dataItem);
        }
    }

    private async Task CreateAndBindItemAsync(object? dataItem)
    {
        var index = _itemCount++;
        var item = await CreateItemAsync(index);

        item.DataItem = dataItem;

        if (NeedDataSource == null)
        {
            item.InvokeTrackViewState(force: true);
        }

        foreach (var cell in item.Cells)
        {
            await cell.Column.InvokeDataBinding(cell, item, _isPostBack);
        }

        await item.DataBindAsync();

        if (ItemDataBound != null)
        {
            await ItemDataBound.InvokeAsync(this, new GridItemEventArgs(item, _isPostBack));
        }

        if (NeedDataSource != null)
        {
            item.InvokeTrackViewState(force: true);
        }
    }

    private async ValueTask<GridItem> CreateItemAsync(int itemIndex)
    {
        var item = new GridItem(itemIndex, this);

        _items.Add(item);
        await Controls.AddAsync(item);

        foreach (var column in Columns)
        {
            var cell = column.CreateCell(Page, item);
            cell.Column = column;
            cell.Grid = this;

            await item.AddCell(cell);
            await column.InvokeItemCreated(cell, item, _isPostBack);
        }

        await item.LoadEditItemTemplateAsync();

        if (ItemCreated != null)
        {
            await ItemCreated.InvokeAsync(this, new GridItemEventArgs(item, _isPostBack));
        }

        return item;
    }

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

    protected override async ValueTask RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (RenderHeader)
        {
            await writer.RenderBeginTagAsync(HtmlTextWriterTag.Thead);
            await writer.RenderBeginTagAsync(HtmlTextWriterTag.Tr);

            foreach (var column in Columns)
            {
                await column.RenderAsync(writer, token);
            }

            await writer.RenderEndTagAsync();
            await writer.RenderEndTagAsync();
        }

        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Tbody);

        foreach (var item in _items)
        {
            await item.RenderAsync(writer, token);
        }

        await writer.RenderEndTagAsync();
    }

    IDataSource? IDataSourceConsumer.DataSource
    {
        get => _dataSource;
        set => _dataSource = value;
    }

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

    int IDataKeyProvider.ItemCount => _itemCount;

    IEnumerable<IDataItemContainer> IDataKeyProvider.Items => _items;

    bool INeedDataSourceProvider.IgnorePaging
    {
        get => _ignorePaging;
        set => _ignorePaging = value;
    }
}
