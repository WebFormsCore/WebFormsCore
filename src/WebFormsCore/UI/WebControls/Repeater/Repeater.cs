using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WebFormsCore.UI.Attributes;

namespace WebFormsCore.UI.WebControls;

public interface IRepeaterItem : IDataItemContainer
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
public class Repeater : RepeaterBase<RepeaterItem>, INeedDataSourceProvider
{
    private bool _ignorePaging;

    public event AsyncEventHandler<Repeater, RepeaterItemEventArgs>? ItemCreated;

    public event AsyncEventHandler<Repeater, RepeaterItemEventArgs>? ItemDataBound;

    public event AsyncEventHandler<Repeater, NeedDataSourceEventArgs>? NeedDataSource;

    protected override ValueTask InvokeNeedDataSource()
    {
        return NeedDataSource.InvokeAsync(this, new NeedDataSourceEventArgs(this, false));
    }

    public override async Task AfterPostBackLoadAsync()
    {
        var count = ItemCount;

        if (NeedDataSource != null)
        {
            if (ItemsAndSeparators.Count != count)
            {
                var filterByKeys = DataKeys.Length > 0;

                await NeedDataSource.InvokeAsync(this, new NeedDataSourceEventArgs(this, filterByKeys));
                _ignorePaging = false;
            }

            if (ItemsAndSeparators.Count != count)
            {
                throw new InvalidOperationException($"The number of items in the repeater ({ItemsAndSeparators.Count}) does not match the number of items in the data source ({count}).");
            }

            Keys.Validate();

            return;
        }

        await base.AfterPostBackLoadAsync();
    }

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

    bool INeedDataSourceProvider.IgnorePaging
    {
        get => _ignorePaging;
        set => _ignorePaging = value;
    }
}

public class Repeater<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T> : Repeater
{
    public new event AsyncEventHandler<Repeater<T>, RepeaterItemEventArgs<T>>? ItemCreated;

    public new event AsyncEventHandler<Repeater<T>, RepeaterItemEventArgs<T>>? ItemDataBound;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type? ItemType
    {
        get => typeof(T);
        set
        {
            // ignore
        }
    }

    public override ValueTask LoadDataSourceAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T1>(object source)
    {
        if (typeof(T1) != typeof(T))
        {
            throw new InvalidOperationException("Cannot load data source of different type.");
        }

        return base.LoadDataSourceAsync<T1>(source);
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
