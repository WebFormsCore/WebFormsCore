using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WebFormsCore.UI.Attributes;

namespace WebFormsCore.UI.WebControls;

public interface IRepeaterItem
{
    ListItemType ItemType { get; }

    Task DataBindAsync();
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

[ParseChildren(true)]
public class Repeater : RepeaterBase<RepeaterItem>
{
    public event AsyncEventHandler<Repeater, RepeaterItemEventArgs>? ItemCreated;

    public event AsyncEventHandler<Repeater, RepeaterItemEventArgs>? ItemDataBound;

    protected override ValueTask<RepeaterItem> CreateItemAsync(int itemIndex, ListItemType itemType)
    {
        return new ValueTask<RepeaterItem>(new RepeaterItem(itemIndex, itemType, this));
    }

    protected override void SetDataItem(RepeaterItem item, object dataItem)
    {
        item.DataItem = dataItem;
    }

    protected override ValueTask InvokeItemDataBound(RepeaterItem item)
    {
        return ItemDataBound.InvokeAsync(this, new RepeaterItemEventArgs(item));
    }

    protected override ValueTask InvokeItemCreated(RepeaterItem item)
    {
        return ItemCreated.InvokeAsync(this, new RepeaterItemEventArgs(item));
    }
}

public class Repeater<T> : Repeater
{
    public new event AsyncEventHandler<Repeater<T>, RepeaterItemEventArgs<T>>? ItemCreated;

    public new event AsyncEventHandler<Repeater<T>, RepeaterItemEventArgs<T>>? ItemDataBound;

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
        if (DataSource is not IAsyncEnumerable<T> asyncEnumerable)
        {
            await base.LoadDataSource();
            return;
        }

        await foreach (var dataItem in asyncEnumerable)
        {
            await CreateItemAsync(true, dataItem);
        }
    }

    protected override ValueTask<RepeaterItem> CreateItemAsync(int itemIndex, ListItemType itemType)
    {
        return new ValueTask<RepeaterItem>(new RepeaterItem<T>(itemIndex, itemType, this));
    }

    protected override void SetDataItem(RepeaterItem item, object dataItem)
    {
        if (dataItem is not T typedDataItem)
        {
            throw new InvalidOperationException("DataItem is not of the correct type.");
        }

        var typedItem = Unsafe.As<RepeaterItem<T>>(item);

        typedItem.DataItem = typedDataItem;
    }

    protected override async ValueTask InvokeItemDataBound(RepeaterItem item)
    {
        await base.InvokeItemDataBound(item);

        var typedItem = Unsafe.As<RepeaterItem<T>>(item);

        await ItemDataBound.InvokeAsync(this, new RepeaterItemEventArgs<T>(typedItem));
    }

    protected override async ValueTask InvokeItemCreated(RepeaterItem item)
    {
        await base.InvokeItemCreated(item);

        var typedItem = Unsafe.As<RepeaterItem<T>>(item);

        await ItemCreated.InvokeAsync(this, new RepeaterItemEventArgs<T>(typedItem));
    }
}
