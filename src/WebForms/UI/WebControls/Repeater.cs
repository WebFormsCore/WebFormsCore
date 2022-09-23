using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls;

public partial class Repeater : Control
{
    [ViewState] private int _itemCount;

    public virtual string? ItemType { get; set; }

    public ITemplate? ItemTemplate { get; set; }

    public IEnumerable? DataSource { get; set; }

    protected override void OnControlViewStateLoaded()
    {
        for (var i = 0; i < _itemCount; i++)
        {
            var item = new RepeaterItem();
            ItemTemplate?.InstantiateIn(item);
            Controls.Add(item);
        }
    }

    public void DataBind()
    {
        Controls.Clear();

        if (DataSource is null)
        {
            return;
        }

        var enumerator = DataSource.GetEnumerator();

        while (enumerator.MoveNext())
        {
            var item = new RepeaterItem();
            ItemTemplate?.InstantiateIn(item);
            Controls.Add(item);
        }

        _itemCount = Controls.Count;
    }
}

public class RepeaterItem : Control
{
}

public class Repeater<T> : Repeater
{
    private IEnumerable<T>? _dataSource;

    public Repeater()
    {
    }

    public new IEnumerable<T>? DataSource
    {
        get => _dataSource;
        set
        {
            base.DataSource = value;
            _dataSource = value;
        }
    }

    public override string? ItemType
    {
        get => typeof(T).FullName;
        set => throw new InvalidOperationException();
    }
}
