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

    protected object? DataSourceField;

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
        await LoadDataSourceEnumerableInner<T>(source);
    }

    public async Task LoadDataSourceAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        T>(IEnumerable<T> source)
    {
        await LoadDataSourceEnumerableInner<T>(source);
    }

    public async Task LoadDataSourceAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        T>(IAsyncEnumerable<T> source)
    {
        await LoadDataSourceEnumerableInner<T>(source);
    }

    private Task LoadDataSourceEnumerableInner<T>(object source)
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

        var (@interface, method, _) = type.GetInterfaces()
            .Where(i => i.IsGenericType)
            .Select(i =>
            {
                MethodInfo? method;
                int order;
                var genericType = i.GetGenericTypeDefinition();

                if (genericType == typeof(IQueryable<>))
                {
                    method = typeof(Grid).GetMethod(nameof(LoadDataSourceQueryable), BindingFlags.NonPublic | BindingFlags.Instance);
                    order = 0;
                }
                else if (genericType == typeof(IAsyncEnumerable<>))
                {
                    method = typeof(Grid).GetMethod(nameof(LoadDataSourceAsyncEnumerable), BindingFlags.NonPublic | BindingFlags.Instance);
                    order = 1;
                }
                else if (genericType == typeof(IEnumerable<>))
                {
                    method = typeof(Grid).GetMethod(nameof(LoadDataSourceEnumerable), BindingFlags.NonPublic | BindingFlags.Instance);
                    order = 2;
                }
                else
                {
                    method = null;
                    order = -1;
                }

                return (Interface: i, Method: method, Order: order);
            })
            .Where(i => i.Method != null)
            .OrderBy(i => i.Order)
            .FirstOrDefault();

        if (method is null)
        {
            throw new InvalidOperationException(
                $"The type {type.FullName} does not implement IQueryable<T>, IAsyncEnumerable<T> or IEnumerable<T>.");
        }

        var elementType = @interface.GetGenericArguments()[0];
        var genericMethod = method.MakeGenericMethod(elementType);

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

        for (var i = 0; i < Columns.Count; i++)
        {
            var cell = item.Cells[i];
            var column = Columns[i];

            await column.InvokeDataBinding(cell, item);
        }

        await item.DataBindAsync();
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

    protected override async Task RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
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
