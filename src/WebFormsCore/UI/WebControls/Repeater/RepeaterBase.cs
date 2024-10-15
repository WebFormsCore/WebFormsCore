using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Providers;

namespace WebFormsCore.UI.WebControls;

public abstract partial class RepeaterBase<TItem> : Control, IPostBackLoadHandler, INamingContainer, IDataKeyProvider, IDataSourceConsumer
    where TItem : Control, IRepeaterItem
{
    private readonly List<(TItem Item, Control? Seperator)> _items = new();
    private Control? _header;
    private Control? _footer;

    protected IReadOnlyList<(TItem Item, Control? Seperator)> ItemsAndSeparators => _items;
    protected Control? Header => _header;
    protected Control? Footer => _footer;

    [ViewState] private bool _loadFromViewState;
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties), ViewState(WriteAlways = true)] private Type? _itemType;
    [ViewState(WriteAlways = true)] private int _itemCount;
    private IDataSource? _dataSource;

    [ViewState] public KeyCollection Keys { get; set; }

    protected RepeaterBase()
    {
        Keys = new KeyCollection(this);
    }

    public string[] DataKeys { get; set; } = Array.Empty<string>();

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public virtual Type? ItemType
    {
        get => _itemType;
        set => _itemType = value;
    }

    public IEnumerable<TItem> Items => _items.Select(x => x.Item);

    public ITemplate? HeaderTemplate { get; set; }

    public ITemplate? FooterTemplate { get; set; }

    public ITemplate? SeparatorTemplate { get; set; }

    public ITemplate? ItemTemplate { get; set; }

    public ITemplate? AlternatingItemTemplate { get; set; }

    public object? DataSource
    {
        get => _dataSource?.Value;
        [RequiresDynamicCode("The element type of the data source is not known at compile time. Use SetDataSource<T>(source) instead.")]
        set => _dataSource = value is not null ? new DataSource(value) : null;
    }

    public virtual async Task AfterPostBackLoadAsync()
    {
        var count = _itemCount;

        if (count == 0)
        {
            return;
        }

        if (_items.Count > 0)
        {
            if (_items.Count != count)
            {
                throw new InvalidOperationException("The number of items in the repeater has changed.");
            }

            return;
        }

        Clear();

        for (var i = 0; i < count; i++)
        {
            await CreateItemAsync();
        }
    }

    protected virtual async ValueTask LoadDataSourceAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(object value, bool dataBinding, bool filterByKeys)
    {
        await LoadDataSourceCoreAsync<T>(value, dataBinding, filterByKeys);

        if (dataBinding)
        {
            Keys.Store();
        }
    }

    ValueTask IDataSourceConsumer.LoadDataSourceAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(object value, bool dataBinding, bool filterByKeys)
    {
        return LoadDataSourceAsync<T>(value, dataBinding, filterByKeys);
    }

    private ValueTask LoadDataSourceCoreAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(object source, bool dataBinding, bool filterByKeys)
    {
        return source switch
        {
            IQueryable<T> queryable => LoadDataSourceQueryable(queryable, dataBinding, filterByKeys),
            IAsyncEnumerable<T> asyncEnumerable => LoadDataSourceAsyncEnumerable(asyncEnumerable, dataBinding, filterByKeys),
            IEnumerable<T> enumerable => LoadDataSourceEnumerable(enumerable, dataBinding, filterByKeys),
            _ => throw new InvalidOperationException($"The type {source.GetType().FullName} does not implement IQueryable<T>, IAsyncEnumerable<T> or IEnumerable<T>.")
        };
    }

    protected virtual ValueTask InvokeNeedDataSource(bool filterByKeys)
    {
        throw new InvalidOperationException("The NeedDataSource event must be handled.");
    }

    public override async ValueTask DataBindAsync(CancellationToken token = default)
    {
        await LoadAsync(dataBinding: true, filterByKeys: false, token);
        await InvokeDataBindingAsync(token);
    }

    protected async ValueTask LoadAsync(bool dataBinding, bool filterByKeys, CancellationToken token = default)
    {
        if (_dataSource is null)
        {
            await InvokeNeedDataSource(filterByKeys);
        }

        if (_dataSource is null)
        {
            throw new InvalidOperationException("The DataSource property must be set.");
        }

        await _dataSource.LoadAsync(this, dataBinding, filterByKeys);
    }

    private async ValueTask LoadDataSourceQueryable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(IQueryable<T> dataSource, bool dataBinding, bool filterByKeys)
    {
        var queryableProvider = Context.RequestServices.GetRequiredService<IQueryableProvider>();

        _itemType = typeof(T);

        Clear();

        var list = await queryableProvider.ToListAsync(dataSource);

        if (filterByKeys)
        {
            Keys.TrySort(list);
        }

        foreach (var dataItem in list)
        {
            await CreateItemAsync(dataBinding, dataItem);
        }

        _itemCount = list.Count;
    }

    private async ValueTask LoadDataSourceEnumerable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(IEnumerable<T> dataSource, bool dataBinding, bool filterByKeys)
    {
        _itemType = typeof(T);

        Clear();

        var i = 0;

        var list = dataSource.ToList();

        if (filterByKeys)
        {
            Keys.TrySort(list);
        }

        foreach (var dataItem in list)
        {
            await CreateItemAsync(dataBinding, dataItem);
            i++;
        }

        _itemCount = i;
    }

    private async ValueTask LoadDataSourceAsyncEnumerable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(IAsyncEnumerable<T> dataSource, bool dataBinding, bool filterByKeys)
    {
        _itemType = typeof(T);

        Clear();

        var i = 0;

        var list = new List<T>();

        await foreach (var dataItem in dataSource)
        {
            list.Add(dataItem);
        }

        if (filterByKeys)
        {
            Keys.TrySort(list);
        }

        foreach (var dataItem in list)
        {
            await CreateItemAsync(dataBinding, dataItem);
            i++;
        }

        _itemCount = i;
    }

    [Obsolete("Use DataBindAsync instead.")]
    public void DataBind()
    {
        DataBindAsync().GetAwaiter().GetResult();
    }

    public async Task AddAsync(object data)
    {
        await CreateItemAsync(true, data);
    }

    public void Remove(TItem item)
    {
        var index = _items.FindIndex(x => x.Item.UniqueID == item.UniqueID);

        if (index == -1)
        {
            throw new InvalidOperationException("Item not found.");
        }

        RemoveAt(index);
    }

    public void RemoveAt(int index)
    {
        var (item, separator) = _items[index];
        _items.RemoveAt(index);
        _itemCount--;

        Controls.Remove(item);

        if (separator is not null)
        {
            Controls.Remove(separator);
        }

        if (_itemCount == 0)
        {
            Clear();
        }
        else
        {
            // Remove the separator of the first item.
            if (index == 0)
            {
                var (firstItem, firstSeparator) = _items[0];

                if (firstSeparator != null)
                {
                    Controls.Remove(firstSeparator);
                    _items[0] = (firstItem, null);
                }
            }
        }
    }

    public void Swap(int index1, int index2)
    {
        (_items[index1], _items[index2]) = (_items[index2], _items[index1]);
    }

    public void Swap(TItem item1, TItem item2)
    {
        var index1 = _items.FindIndex(x => x.Item == item1);
        var index2 = _items.FindIndex(x => x.Item == item2);

        if (index1 == -1 || index2 == -1)
        {
            throw new InvalidOperationException("Item not found.");
        }

        Swap(index1, index2);
    }

    /// <summary>
    /// Updates the names of the items and separators so they are the same when the page is posted back.
    /// </summary>
    private void UpdateNames()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            var (item, separator) = _items[i];

            item.ID = $"i{i}";

            if (separator is not null)
            {
                separator.ID = $"s{i}";
            }
        }
    }

    protected override ValueTask OnPreRenderAsync(CancellationToken token)
    {
        UpdateNames();
        return base.OnPreRenderAsync(token);
    }

    protected override async ValueTask RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (_header is not null)
        {
            await _header.RenderAsync(writer, token);
        }

        foreach (var (item, separator) in _items)
        {
            if (separator is not null)
            {
                await separator.RenderAsync(writer, token);
            }

            await item.RenderAsync(writer, token);
        }

        if (_footer is not null)
        {
            await _footer.RenderAsync(writer, token);
        }
    }

    private void Clear()
    {
        _itemCount = 0;
        _items.Clear();
        _header = null;
        _footer = null;
        Controls.Clear();
    }

    public TItem this[int index] => _items[index].Item;

    protected virtual void InitializeItem(TItem item)
    {
        var contentTemplate = item.ItemType switch
        {
            ListItemType.Header => HeaderTemplate,
            ListItemType.Footer => FooterTemplate,
            ListItemType.Item => ItemTemplate,
            ListItemType.AlternatingItem => AlternatingItemTemplate ?? ItemTemplate,
            ListItemType.Separator => SeparatorTemplate,
            _ => null
        };

        contentTemplate?.InstantiateIn(item);
    }

    protected virtual async ValueTask<TItem> CreateItemAsync(bool dataBinding = false, object? dataItem = default)
    {
        if (_header == null && HeaderTemplate != null)
        {
            _header = await CreateItemAsync(ListItemType.Header, true);
            _header.ID = "h";
        }

        Control? separator = null;

        var index = _itemCount;

        if (index > 0)
        {
            separator = await CreateItemAsync(ListItemType.Separator);
            separator.ID = $"s{index}";
        }

        var itemType = (index % 2 == 0) ? ListItemType.Item : ListItemType.AlternatingItem;
        var item = await CreateItemAsync(itemType, dataBinding, dataItem);

        item.ID = $"i{index}";
        _items.Add((item, separator));

        if (_footer == null && FooterTemplate != null)
        {
            _footer = await CreateItemAsync(ListItemType.Footer, true);
            _footer.ID = "f";
        }

        return item;
    }

    private async ValueTask<TItem> CreateItemAsync(ListItemType itemType, bool dataBinding = false, object? dataItem = default)
    {
        var itemIndex = itemType is ListItemType.Item or ListItemType.AlternatingItem ? _itemCount++ : -1;
        var item = await CreateItemAsync(itemIndex, itemType);

        Controls.AddWithoutPageEvents(item);
        InitializeItem(item);

        if (dataItem != null)
        {
            SetDataItem(item, dataItem!);
        }

        await item.DataBindAsync();

        await InvokeItemCreated(item);

        var state = _state;
        if (state != ControlState.Constructed)
        {
            await AddedControlAsync(state, item);
        }

        if (dataBinding)
        {
            await item.DataBindAsync();
            await InvokeItemDataBound(item);
        }

        return item;
    }

    public override void AddParsedSubObject(Control control)
    {
        // ignore
    }

    protected abstract ValueTask<TItem> CreateItemAsync(int itemIndex, ListItemType itemType);

    protected abstract void SetDataItem(TItem item, object dataItem);

    protected abstract ValueTask InvokeItemDataBound(TItem item);

    protected abstract ValueTask InvokeItemCreated(TItem item);

    protected int ItemCount => _itemCount;

    int IDataKeyProvider.ItemCount => _itemCount;

    IEnumerable<IDataItemContainer> IDataKeyProvider.Items => _items.Select(x => x.Item);

    IDataSource? IDataSourceConsumer.DataSource
    {
        get => _dataSource;
        set => _dataSource = value;
    }
}
