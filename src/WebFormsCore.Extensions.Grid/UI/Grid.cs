using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using WebFormsCore.UI.Attributes;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI;

[ParseChildren(true)]
[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
public partial class Grid : WebControl, IPostBackLoadHandler
{
    private readonly List<GridItem> _items = new();

    [ViewState] private int _pageIndex;
    [ViewState] private int _itemCount;
    [ViewState] private int? _dataCount;
    [ViewState] private Type? _itemType;
    [ViewState] public AttributeCollection RowAttributes { get; set; } = new();
    [ViewState] public AttributeCollection EditRowAttributes { get; set; } = new();

    protected object? DataSourceField;

    public event AsyncEventHandler<Grid, GridItemEventArgs>? ItemCreated;

    public event AsyncEventHandler<Grid, GridItemEventArgs>? ItemDataBound;

    public ITemplate? EditItemTemplate { get; set; }

    public Grid()
        : base(HtmlTextWriterTag.Table)
    {
    }

    public virtual Type? ItemType => _itemType;

    public object? DataSource
    {
        get => DataSourceField;
        [RequiresDynamicCode("The type of the data source is not known at compile time. Use LoadDataSourceAsync<T>(source) instead.")]
        set => DataSourceField = value;
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
        var count = _itemCount;

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

    public async Task LoadDataSourceAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        T>(IQueryable<T> source)
    {
        await LoadDataSourceInnerAsync<T>(source);
    }

    public async Task LoadDataSourceAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        T>(IEnumerable<T> source)
    {
        await LoadDataSourceInnerAsync<T>(source);
    }

    public async Task LoadDataSourceAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        T>(IAsyncEnumerable<T> source)
    {
        await LoadDataSourceInnerAsync<T>(source);
    }

    private Task LoadDataSourceInnerAsync<T>(object source)
    {
        return source switch
        {
            IQueryable<T> queryable => LoadDataSourceQueryable(queryable),
            IAsyncEnumerable<T> asyncEnumerable => LoadDataSourceAsyncEnumerable(asyncEnumerable),
            IEnumerable<T> enumerable => LoadDataSourceEnumerable(enumerable),
            _ => throw new InvalidOperationException($"The type {source.GetType().FullName} does not implement IQueryable<T>, IAsyncEnumerable<T> or IEnumerable<T>.")
        };
    }

    [RequiresDynamicCode("The type of the data source is not known at compile time. Use LoadDataSourceAsync<T>(source) instead.")]
    public async Task DataBindAsync()
    {
        await LoadDataSourceAsync();
    }

    protected virtual async Task LoadDataSourceAsync()
    {
        if (DataSourceField is null)
        {
            await ClearAsync();
            return;
        }

        var type = DataSourceField.GetType();
        var elementType = type.GetInterfaces()
            .Where(i => i.IsGenericType)
            .FirstOrDefault(i =>
            {
                var genericType = i.GetGenericTypeDefinition();

                return genericType == typeof(IQueryable<>) ||
                       genericType == typeof(IAsyncEnumerable<>) ||
                       genericType == typeof(IEnumerable<>);
            })?.GetGenericArguments()[0];

        if (elementType is null)
        {
            throw new InvalidOperationException(
                $"The type {type.FullName} does not implement IQueryable<T>, IAsyncEnumerable<T> or IEnumerable<T>.");
        }

        var genericMethod = typeof(Grid)
            .GetMethod(nameof(LoadDataSourceInnerAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(elementType);

        await (Task)genericMethod.Invoke(this, new[] { DataSourceField })!;
    }

    protected async Task LoadDataSourceQueryable<T>(IQueryable<T> dataSource)
    {
        _itemType = typeof(T);

        if (dataSource is IAsyncEnumerable<T> allEnumerable)
        {
            _dataCount = await allEnumerable.CountAsync();
        }
        else
        {
            _dataCount = dataSource.Count();
        }

        UpdatePaging();

        if (PageSize.HasValue)
        {
            if (PageIndex > 0)
            {
                dataSource = dataSource.Skip(PageIndex * PageSize.Value);
            }

            dataSource = dataSource.Take(PageSize.Value);
        }

        await ClearAsync();

        if (dataSource is IAsyncEnumerable<T> asyncEnumerable)
        {
            await foreach (var dataItem in asyncEnumerable)
            {
                await CreateAndBindItemAsync(dataItem);
            }
        }
        else
        {
            foreach (var dataItem in dataSource)
            {
                await CreateAndBindItemAsync(dataItem);
            }
        }
    }

    protected async Task LoadDataSourceEnumerable<T>(IEnumerable<T> dataSource)
    {
        _itemType = typeof(T);
        _dataCount = dataSource.Count();

        UpdatePaging();

        if (PageSize.HasValue)
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

    protected async Task LoadDataSourceAsyncEnumerable<T>(IAsyncEnumerable<T> dataSource)
    {
        _itemType = typeof(T);
        _dataCount = await dataSource.CountAsync();

        UpdatePaging();

        if (PageSize.HasValue)
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

        // Force tracking since the data item is not available on postback
        item.InvokeTrackViewState();

        foreach (var cell in item.Cells)
        {
            await cell.Column.InvokeDataBinding(cell, item);
        }

        await item.DataBindAsync();

        if (ItemDataBound != null)
        {
            await ItemDataBound.InvokeAsync(this, new GridItemEventArgs(item));
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
            await column.InvokeItemCreated(cell, item);
        }

        await item.LoadEditItemTemplateAsync();

        if (ItemCreated != null)
        {
            await ItemCreated.InvokeAsync(this, new GridItemEventArgs(item));
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
}

[ParseChildren(true)]
public partial class Grid<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    T> : Grid
{
    public override Type ItemType => base.ItemType ?? typeof(T);

    protected override Task LoadDataSourceAsync()
    {
        return DataSourceField switch
        {
            IQueryable<T> queryable => LoadDataSourceQueryable(queryable),
            IAsyncEnumerable<T> asyncEnumerable => LoadDataSourceAsyncEnumerable(asyncEnumerable),
            IEnumerable<T> enumerable => LoadDataSourceEnumerable(enumerable),
            _ => base.LoadDataSourceAsync()
        };
    }
}
