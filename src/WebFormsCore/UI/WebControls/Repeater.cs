using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls;

public abstract partial class RepeaterBase<TItem, TEventArgs> : Control, IPostBackLoadHandler
    where TItem : RepeaterItem
{
    private readonly List<TItem> _items = new();

    [ViewState] private int _itemCount;

    public virtual string? ItemType { get; set; }

    public IReadOnlyList<TItem> Items => _items;

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

        if (HeaderTemplate != null) await CreateItemAsync(ListItemType.Header);

        for (var i = 0; i < count; i++)
        {
            await CreateItemAsync();
        }

        if (FooterTemplate != null) await CreateItemAsync(ListItemType.Footer);
    }

    public async Task DataBindAsync()
    {
        Clear();

        if (HeaderTemplate != null) await CreateItemAsync(ListItemType.Header, true);

        await LoadDataSource();

        if (FooterTemplate != null) await CreateItemAsync(ListItemType.Footer, true);
    }

    [Obsolete("Use DataBindAsync instead.")]
    public void DataBind()
    {
        DataBindAsync().GetAwaiter().GetResult();
    }

    public async Task AddAsync(object data)
    {
        if (_itemCount == 0 && HeaderTemplate != null)
        {
            await CreateItemAsync(ListItemType.Header, true);
        }

        TItem? footer = null;
        if (FooterTemplate != null && _itemCount > 0)
        {
            var index = _items.Count - 1;
            footer = _items[index];
            _items.RemoveAt(index);
            Controls.Remove(footer);
        }

        await CreateItemAsync(true, data);

        if (footer != null)
        {
            _items.Add(footer);
            Controls.Add(footer);
        }
        else if (FooterTemplate != null)
        {
            await CreateItemAsync(ListItemType.Footer, true);
        }
    }

    protected override async Task RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        foreach (var item in _items)
        {
            await item.RenderAsync(writer, token);
        }
    }

    private void Clear()
    {
        _itemCount = 0;
        _items.Clear();
        Controls.Clear();
    }

    protected virtual async Task LoadDataSource()
    {
        if (DataSource is not IEnumerable dataSource) return;

        foreach (var dataItem in dataSource)
        {
            await CreateItemAsync(true, dataItem);
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

    private ValueTask<TItem> CreateItemAsync(bool useDataSource = false, object? dataItem = null)
    {
        var itemType = (_itemCount % 2 == 0) ? ListItemType.Item : ListItemType.AlternatingItem;
        return CreateItemAsync(itemType, useDataSource, dataItem);
    }

    private async ValueTask<TItem> CreateItemAsync(ListItemType itemType, bool dataBind = false, object? dataItem = null)
    {
        int itemIndex;

        if (itemType is ListItemType.Item or ListItemType.AlternatingItem)
        {
            if (_itemCount > 0)
            {
                await CreateItemAsync(ListItemType.Separator);
            }

            itemIndex = _itemCount++;
        }
        else
        {
            itemIndex = -1;
        }

        var item = CreateItem(itemIndex, itemType);

        _items.Add(item);
        InitializeItem(item);
        if (dataBind)
        {
            item.DataItem = dataItem;
        }

        await ItemCreated.InvokeAsync(this, CreateEventArgs(item));
        Controls.Add(item);

        if (dataBind)
        {
            await item.DataBindAsync();
            await ItemDataBound.InvokeAsync(this, CreateEventArgs(item));

            item.DataItem = null;
        }

        return item;
    }

    public override void AddParsedSubObject(Control control)
    {
        // ignore
    }

    protected abstract TItem CreateItem(int itemIndex, ListItemType itemType);

    protected abstract TEventArgs CreateEventArgs(TItem item);
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

public class Repeater : RepeaterBase<RepeaterItem, RepeaterItemEventArgs>
{
    protected override RepeaterItem CreateItem(int itemIndex, ListItemType itemType)
    {
        return new RepeaterItem(itemIndex, itemType);
    }

    protected override RepeaterItemEventArgs CreateEventArgs(RepeaterItem item)
    {
        return new RepeaterItemEventArgs(item);
    }
}

public class Repeater<T> : RepeaterBase<RepeaterItem<T>, RepeaterItemEventArgs<T>>
{
    public override string? ItemType
    {
        get => typeof(T).FullName;
        set
        {
            // ignore
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
}
