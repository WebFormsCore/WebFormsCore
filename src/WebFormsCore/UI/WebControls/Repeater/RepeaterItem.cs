using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls;

public class RepeaterItem : Control, IRepeaterItem
{
    private object? _dataItem;

    public override string UniqueID => Parent.UniqueID + IdSeparator + base.UniqueID;

    public override string ClientID => Parent.ClientID + '_' + base.ClientID;

    public RepeaterItem(int itemIndex, ListItemType itemType, Repeater repeater)
    {
        ItemIndex = itemIndex;
        ItemType = itemType;
        Repeater = repeater;
    }

    public Repeater Repeater { get; }

    protected virtual object? GetDataItem() => _dataItem;

    protected virtual void SetDataItem(object? value) => _dataItem = value;

    public virtual object? DataItem
    {
        get => GetDataItem();
        set => SetDataItem(value);
    }

    public virtual int ItemIndex { get; set; }

    public virtual ListItemType ItemType { get; set; }

    public virtual Task DataBindAsync()
    {
        return Task.CompletedTask;
    }

    int IDataItemContainer.DataItemIndex => ItemIndex;

    int IDataItemContainer.DisplayIndex => ItemIndex;
}

public class RepeaterItem<T> : RepeaterItem
{
    private T? _dataItem;

    public RepeaterItem(int itemIndex, ListItemType itemType, Repeater<T> repeater)
        : base(itemIndex, itemType, repeater)
    {
        Repeater = repeater;
    }

    public new Repeater<T> Repeater { get; }

    public new T? DataItem
    {
        get => _dataItem;
        set => _dataItem = value!;
    }

    protected override object? GetDataItem() => _dataItem;

    protected override void SetDataItem(object? value) => _dataItem = (T) value!;
}
