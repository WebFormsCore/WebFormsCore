using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls;

public abstract partial class RepeaterBase<T, TItem, TEventArgs> : Control, IPostBackLoadHandler, INamingContainer
    where TItem : RepeaterItem
{
    private readonly List<(TItem Item, Control? Seperator)> _items = new();
    private Control? _header;
    private Control? _footer;

    [ViewState] private int _itemCount;

    public virtual string? ItemType { get; set; }

    public IEnumerable<TItem> Items => _items.Select(x => x.Item);

    public event AsyncEventHandler<TEventArgs>? ItemCreated;

    public event AsyncEventHandler<TEventArgs>? ItemDataBound;

    public ITemplate? HeaderTemplate { get; set; }

    public ITemplate? FooterTemplate { get; set; }

    public ITemplate? SeparatorTemplate { get; set; }

    public ITemplate? ItemTemplate { get; set; }

    public ITemplate? AlternatingItemTemplate { get; set; }

    public object? DataSource { get; set; }

    public async Task AfterPostBackLoadAsync()
    {
        var count = _itemCount;

        if (count == 0)
        {
            return;
        }

        Clear();

        for (var i = 0; i < count; i++)
        {
            await CreateItemAsync();
        }
    }

    public async Task DataBindAsync()
    {
        Clear();
        await LoadDataSource();
    }

    [Obsolete("Use DataBindAsync instead.")]
    public void DataBind()
    {
        DataBindAsync().GetAwaiter().GetResult();
    }

    public async Task AddAsync(T data)
    {
        await CreateItemAsync(true, data);
    }

    public void Remove(RepeaterItem item)
    {
        var index = _items.FindIndex(x => x.Item == item);

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

    protected override async Task RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
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

    protected virtual async Task LoadDataSource()
    {
        if (DataSource is null)
        {
            return;
        }

        if (DataSource is not IEnumerable dataSource)
        {
            throw new InvalidOperationException("DataSource is not an IEnumerable.");
        }

        foreach (var dataItem in dataSource)
        {
            if (dataItem is not T dataObject)
            {
                throw new InvalidOperationException("DataSource item is not of the correct type.");
            }

            await CreateItemAsync(true, dataObject);
        }
    }

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

    protected async ValueTask<TItem> CreateItemAsync(bool useDataSource = false, T? dataItem = default)
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
        var item = await CreateItemAsync(itemType, useDataSource, dataItem);

        item.ID = $"i{index}";
        _items.Add((item, separator));

        if (_footer == null && FooterTemplate != null)
        {
            _footer = await CreateItemAsync(ListItemType.Footer, true);
            _footer.ID = "f";
        }

        return item;
    }

    private async ValueTask<TItem> CreateItemAsync(ListItemType itemType, bool dataBind = false, T? dataItem = default)
    {
        var itemIndex = itemType is ListItemType.Item or ListItemType.AlternatingItem ? _itemCount++ : -1;
        var item = CreateItem(itemIndex, itemType);

        InitializeItem(item);
        if (dataBind)
        {
            SetDataItem(item, dataItem!);
        }

        await ItemCreated.InvokeAsync(this, CreateEventArgs(item));
        await Controls.AddAsync(item);

        if (dataBind)
        {
            await item.DataBindAsync();
            await ItemDataBound.InvokeAsync(this, CreateEventArgs(item));
        }

        return item;
    }

    public override void AddParsedSubObject(Control control)
    {
        // ignore
    }

    protected abstract TItem CreateItem(int itemIndex, ListItemType itemType);

    protected abstract TEventArgs CreateEventArgs(TItem item);

    protected abstract void SetDataItem(TItem item, T dataItem);
}

public enum ListItemType
{
    Header = 0,
    Footer = 1,
    Item = 2,
    AlternatingItem = 3,
    SelectedItem = 4,
    EditItem = 5,
    Separator = 6,
    Pager = 7
}

public class Repeater : RepeaterBase<object, RepeaterItem, RepeaterItemEventArgs>
{
    protected override RepeaterItem CreateItem(int itemIndex, ListItemType itemType)
    {
        return new RepeaterItem(itemIndex, itemType);
    }

    protected override RepeaterItemEventArgs CreateEventArgs(RepeaterItem item)
    {
        return new RepeaterItemEventArgs(item);
    }

    protected override void SetDataItem(RepeaterItem item, object dataItem)
    {
        item.DataItem = dataItem;
    }
}

public class Repeater<T> : RepeaterBase<T, RepeaterItem<T>, RepeaterItemEventArgs<T>>
{
    public override string? ItemType
    {
        get => typeof(T).FullName;
        set
        {
            // ignore
        }
    }

    protected override async Task LoadDataSource()
    {
        switch (DataSource)
        {
            case IAsyncEnumerable<T> asyncEnumerable:
                await foreach (var dataItem in asyncEnumerable)
                {
                    await CreateItemAsync(true, dataItem);
                }

                break;
            case IEnumerable<T> enumerable:
                foreach (var dataItem in enumerable)
                {
                    await CreateItemAsync(true, dataItem);
                }

                break;
            default:
                await base.LoadDataSource();
                break;
        }
    }

    protected override RepeaterItem<T> CreateItem(int itemIndex, ListItemType itemType)
    {
        return new RepeaterItem<T>(itemIndex, itemType);
    }

    protected override RepeaterItemEventArgs<T> CreateEventArgs(RepeaterItem<T> item)
    {
        return new RepeaterItemEventArgs<T>(item);
    }

    protected override void SetDataItem(RepeaterItem<T> item, T dataItem)
    {
        item.DataItem = dataItem;
    }
}
