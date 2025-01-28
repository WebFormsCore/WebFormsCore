using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WebFormsCore.UI.Attributes;

namespace WebFormsCore.UI.WebControls;

public interface IRepeaterItem : IDataItemContainer
{
    ListItemType ItemType { get; }
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
    Pager = 7,
    NoData = 8
}

[ParseChildren(true)]
public class Repeater : RepeaterBase<RepeaterItem>
{
    public ITemplate? HeaderTemplate { get; set; }

    public ITemplate? FooterTemplate { get; set; }

    public ITemplate? SeparatorTemplate { get; set; }

    public ITemplate? ItemTemplate { get; set; }

    public ITemplate? AlternatingItemTemplate { get; set; }

    public ITemplate? NoDataTemplate { get; set; }

    public event AsyncEventHandler<Repeater, RepeaterItemEventArgs>? ItemCreated;

    public event AsyncEventHandler<Repeater, RepeaterItemEventArgs>? ItemDataBound;

    public event AsyncEventHandler<Repeater, NeedDataSourceEventArgs>? NeedDataSource;

    protected override ValueTask InvokeNeedDataSource(bool filterByKeys)
    {
        return NeedDataSource.InvokeAsync(this, new NeedDataSourceEventArgs(this, filterByKeys));
    }

    protected override void InitializeItem(RepeaterItem item)
    {
        var contentTemplate = item.ItemType switch
        {
            ListItemType.Header => HeaderTemplate,
            ListItemType.Footer => FooterTemplate,
            ListItemType.Item => ItemTemplate,
            ListItemType.AlternatingItem => AlternatingItemTemplate ?? ItemTemplate,
            ListItemType.Separator => SeparatorTemplate,
            ListItemType.NoData => NoDataTemplate,
            _ => null
        };

        contentTemplate?.InstantiateIn(item);
    }

    protected override ValueTask<RepeaterItem?> CreateItemAsync(int itemIndex, ListItemType itemType)
    {
        var hasTemplate = itemType switch
        {
            ListItemType.Header => HeaderTemplate != null,
            ListItemType.Footer => FooterTemplate != null,
            ListItemType.Item => ItemTemplate != null,
            ListItemType.AlternatingItem => AlternatingItemTemplate != null || ItemTemplate != null,
            ListItemType.Separator => SeparatorTemplate != null,
            ListItemType.NoData => NoDataTemplate != null,
            _ => false
        };

        return hasTemplate
            ? new ValueTask<RepeaterItem?>(new RepeaterItem(itemIndex, itemType, this))
            : default;
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

    protected override ValueTask LoadDataSourceAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T1>(object value, bool dataBinding, bool filterByKeys)
    {
        if (typeof(T1) != typeof(T))
        {
            throw new InvalidOperationException("Cannot load data source of different type.");
        }

        return base.LoadDataSourceAsync<T1>(value, dataBinding, filterByKeys);
    }

    protected override ValueTask<RepeaterItem?> CreateItemAsync(int itemIndex, ListItemType itemType)
    {
        return new ValueTask<RepeaterItem?>(new RepeaterItem<T>(itemIndex, itemType, this));
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
