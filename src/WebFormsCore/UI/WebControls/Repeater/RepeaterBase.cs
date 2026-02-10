using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Providers;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI.WebControls;

public abstract partial class RepeaterBase<TItem> : Control, IPostBackAsyncLoadHandler, INamingContainer, IDataKeyProvider, IDataSourceConsumer, INeedDataSourceProvider
    where TItem : Control, IRepeaterItem
{
    private bool _ignorePaging;

    private readonly List<(TItem Item, Control? Seperator)> _items = new();
    private Control? _header;
    private Control? _footer;
    private Control? _empty;

    protected IReadOnlyList<(TItem Item, Control? Seperator)> ItemsAndSeparators => _items;
    protected Control? Header => _header;
    protected Control? Footer => _footer;

    public bool LoadDataOnPostBack { get; set; }

    /// <summary>
    /// Gets or sets the number of skeleton placeholder items to render when
    /// the repeater is inside a <see cref="Skeleton.SkeletonContainer"/> or
    /// <see cref="Skeleton.LazyLoader"/> and has no data yet. Defaults to 3.
    /// </summary>
    public int SkeletonItemCount { get; set; } = 3;

    [ViewState] private bool _loadFromViewState;
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties), ViewState(WriteAlways = true)] private Type? _itemType;
    [ViewState(WriteAlways = true)] private int _itemCount;
    private IDataSource? _dataSource;

    [ViewState] public KeyCollection Keys { get; set; }

    protected RepeaterBase()
    {
        Keys = new KeyCollection(this);
        Items = new ReadOnlyList(_items);
    }

    public int? PageSize { get; set; }

    public int PageIndex { get; set; }

    [ViewState(WriteAlways = true)] public int PageCount { get; private set; }

    public string[] DataKeys { get; set; } = [];

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public virtual Type? ItemType
    {
        get => _itemType;
        set => _itemType = value;
    }

    public ReadOnlyList Items { get; }

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
            _empty ??= await CreateItemAsync(ListItemType.NoData);
            return;
        }

        if (LoadDataOnPostBack && ItemsAndSeparators.Count != count)
        {
            await LoadAsync(dataBinding: false, filterByKeys: true);
            _ignorePaging = true;

            if (ItemsAndSeparators.Count != count)
            {
                throw new InvalidOperationException($"The number of items in the repeater ({ItemsAndSeparators.Count}) does not match the number of items in the data source ({count}).");
            }

            Keys.Validate();

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

        await BeforeDataBindAsync();
        Clear();

        for (var i = 0; i < count; i++)
        {
            await CreateItemAsync();
        }
    }

    protected virtual async ValueTask LoadDataSourceAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(object value, bool dataBinding, bool filterByKeys)
    {
        await BeforeDataBindAsync();
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

#pragma warning disable WFC0001
    public override async ValueTask DataBindAsync(CancellationToken token = default)
    {
        await LoadAsync(dataBinding: true, filterByKeys: false, token);
        await InvokeDataBindingAsync(token);
    }
#pragma warning restore WFC0001

    protected async ValueTask LoadAsync(bool dataBinding, bool filterByKeys, CancellationToken token = default)
    {
        if (_dataSource is null)
        {
            await InvokeNeedDataSource(filterByKeys);
        }

        if (_dataSource is null)
        {
            return;
        }

        await _dataSource.LoadAsync(this, dataBinding, filterByKeys);
    }

    private async ValueTask LoadDataSourceQueryable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(IQueryable<T> dataSource, bool dataBinding, bool filterByKeys)
    {
        var queryableProvider = Context.RequestServices.GetRequiredService<IQueryableProvider>();

        _itemType = typeof(T);

        Clear();

        if (!_ignorePaging && PageSize.HasValue)
        {
            var count = await queryableProvider.CountAsync(dataSource);
            PageCount = (int)Math.Ceiling(count / (double)PageSize.Value);

            dataSource = dataSource.Skip(PageIndex * PageSize.Value).Take(PageSize.Value);
        }

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

        if (!_ignorePaging && PageSize.HasValue)
        {
            // ReSharper disable once PossibleMultipleEnumeration
            var count = dataSource.Count();
            PageCount = (int)Math.Ceiling(count / (double)PageSize.Value);

            // ReSharper disable once PossibleMultipleEnumeration
            dataSource = dataSource.Skip(PageIndex * PageSize.Value).Take(PageSize.Value);
        }

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

        if (!_ignorePaging && PageSize.HasValue)
        {
            // ReSharper disable once PossibleMultipleEnumeration
            var count = await dataSource.CountAsync();
            PageCount = (int)Math.Ceiling(count / (double)PageSize.Value);

            // ReSharper disable once PossibleMultipleEnumeration
            dataSource = dataSource.Skip(PageIndex * PageSize.Value).Take(PageSize.Value);
        }

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
        await BeforeDataBindAsync();
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

            UpdateNames();
        }
    }

    public void Swap(int index1, int index2)
    {
        (_items[index1], _items[index2]) = (_items[index2], _items[index1]);
        UpdateNames();
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

    protected override async ValueTask OnPreRenderAsync(CancellationToken token)
    {
        if (_items.Count == 0)
        {
            _empty ??= await CreateItemAsync(ListItemType.NoData, true);
        }

        UpdateNames();
        await base.OnPreRenderAsync(token);
    }

    protected override async ValueTask RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (_empty is not null)
        {
            await _empty.RenderAsync(writer, token);
            return;
        }

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
        _empty = null;
        Controls.Clear();
    }

    public TItem this[int index] => _items[index].Item;

    protected abstract void InitializeItem(TItem item);

    protected virtual async ValueTask<TItem?> CreateItemAsync(bool dataBinding = false, object? dataItem = default)
    {
        if (_empty is not null)
        {
            Controls.Remove(_empty);
            _empty = null;
        }

        _header ??= await CreateItemAsync(ListItemType.Header, true);
        _footer ??= await CreateItemAsync(ListItemType.Footer, true);

        Control? separator = null;

        var index = _itemCount;

        if (index > 0)
        {
            separator = await CreateItemAsync(ListItemType.Separator);
        }

        var itemType = (index % 2 == 0) ? ListItemType.Item : ListItemType.AlternatingItem;
        var item = await CreateItemAsync(itemType, dataBinding, dataItem);

        if (item is not null)
        {
            _items.Add((item, separator));
        }

        return item;
    }

    private async ValueTask<TItem?> CreateItemAsync(ListItemType itemType, bool dataBinding = false, object? dataItem = default)
    {
        var itemIndex = itemType switch
        {
            ListItemType.Item or ListItemType.AlternatingItem => _itemCount++,
            ListItemType.Separator => _itemCount,
            _ => -1
        };
        var item = await CreateItemAsync(itemIndex, itemType);

        if (item is null)
        {
            return null;
        }

        item.ID = itemType switch
        {
            ListItemType.Header => "h",
            ListItemType.Footer => "f",
            ListItemType.Item or ListItemType.AlternatingItem => $"i{itemIndex}",
            ListItemType.Separator => $"s{itemIndex}",
            ListItemType.NoData => "n",
            _ => throw new ArgumentOutOfRangeException(nameof(itemType))
        };

        Controls.AddWithoutPageEvents(item);
        InitializeItem(item);

        if (dataItem != null)
        {
            SetDataItem(item, dataItem);
        }

        await InvokeItemCreated(item);

        var state = _state;
        if (state != ControlState.Constructed)
        {
            await InvokeStateMethodsAsync(state, item);
        }

        if (dataBinding)
        {
            item.InvokeTrackViewState(force: true);
            await item.DataBindAsync();
            await InvokeItemDataBound(item);
        }

        return item;
    }

    public override void AddParsedSubObject(Control control)
    {
        // ignore
    }

    protected virtual ValueTask BeforeDataBindAsync()
    {
        return default;
    }

    protected abstract ValueTask<TItem?> CreateItemAsync(int itemIndex, ListItemType itemType);

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

    bool INeedDataSourceProvider.IgnorePaging
    {
        get => _ignorePaging;
        set => _ignorePaging = value;
    }

    public class ReadOnlyList(List<(TItem Item, Control? Seperator)> items) : IReadOnlyList<TItem>
    {
        public Enumerator GetEnumerator()
        {
            return new Enumerator(items.GetEnumerator());
        }

        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => items.Count;

        public TItem this[int index] => items[index].Item;

        public struct Enumerator(List<(TItem Item, Control? Seperator)>.Enumerator enumerator) : IEnumerator<TItem>
        {
            private List<(TItem Item, Control? Seperator)>.Enumerator _enumerator = enumerator;

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                ((IEnumerator)_enumerator).Reset();
            }

            public TItem Current => _enumerator.Current.Item;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                _enumerator.Dispose();
            }
        }
    }

}
