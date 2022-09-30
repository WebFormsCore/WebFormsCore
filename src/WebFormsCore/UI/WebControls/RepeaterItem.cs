using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls;

public class RepeaterItem : Control, IDataItemContainer
{
    private object? _dataItem;

    public RepeaterItem(int itemIndex, ListItemType itemType)
    {
        ItemIndex = itemIndex;
        ItemType = itemType;
    }

    public virtual object? DataItem
    {
        get => _dataItem;
        set
        {
            _dataItem = value;
            OnDataItemChanged();
        }
    }

    public virtual int ItemIndex { get; set; }

    public virtual ListItemType ItemType { get; set; }

    public virtual Task DataBindAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual void OnDataItemChanged()
    {
    }

    int IDataItemContainer.DataItemIndex => ItemIndex;

    int IDataItemContainer.DisplayIndex => ItemIndex;
}

public class RepeaterItem<T> : RepeaterItem
{
    private T? _dataItem;

    public RepeaterItem(int itemIndex, ListItemType itemType)
        : base(itemIndex, itemType)
    {
    }

    public new T? DataItem
    {
        get => _dataItem;
        set
        {
            _dataItem = value!;
            base.DataItem = value;
        }
    }

    protected override void OnDataItemChanged()
    {
        _dataItem = (T?)base.DataItem;
    }
}
