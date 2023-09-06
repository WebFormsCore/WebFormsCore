﻿using System.Reflection;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI;

public partial class TableCell : WebControl
{
    public TableCell()
        : base(HtmlTextWriterTag.Td)
    {
    }

    protected override bool GenerateAutomaticID => false;

    [ViewState] public string? Text { get; set; }

    public override void ClearControl()
    {
        base.ClearControl();
        Text = null;
    }

    protected override Task RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return HasRenderingData()
            ? base.RenderContentsAsync(writer, token)
            : writer.WriteAsync(Text);
    }
}

public class TableRow : WebControl
{
    public TableRow()
        : base(HtmlTextWriterTag.Tr)
    {
    }

    protected override bool GenerateAutomaticID => false;
}

public class GridCell : TableCell
{
    public int ColumnIndex { get; set; }

    public Grid Grid { get; set; } = null!;

    public GridColumn Column { get; set; } = null!;

    public IGridCellRenderer? Renderer { get; set; }

    public PropertyInfo? PropertyInfo { get; set; }

    public override void ClearControl()
    {
        base.ClearControl();

        Grid = null!;
        Column = null!;
    }

    protected override Task AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        Column.CellAttributes.AddAttributes(writer);
        return base.AddAttributesToRender(writer, token);
    }

    public override Task RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (!Column.Visible) return Task.CompletedTask;

        return base.RenderAsync(writer, token);
    }
}

public partial class GridItem : TableRow, IDataItemContainer
{
    internal readonly List<GridCell> Cells = new();
    private object? _dataItem;

    protected override bool GenerateAutomaticID => false;

    public override string UniqueID => $"{Parent.UniqueID}{IdSeparator}tr{ItemIndex}";

    public override string ClientID => $"{Parent.ClientID}_tr{ItemIndex}";

    public override bool EnableViewState
    {
        get => base.EnableViewState;
        set => base.EnableViewState = value;
    }

    public GridItemEditContainer? EditItemTemplateContainer { get; private set; }

    public GridItem(int itemIndex, Grid grid)
    {
        ItemIndex = itemIndex;
        Grid = grid;
    }

    [ViewState] public bool Selected { get; set; }

    public bool EditMode
    {
        get => EditItemTemplateContainer?.Visible ?? false;
        set
        {
            if (EditItemTemplateContainer != null)
            {
                EditItemTemplateContainer.Visible = value;
            }
        }
    }

    public Grid Grid { get; }

    protected virtual object? GetDataItem() => _dataItem;

    protected virtual void SetDataItem(object? value) => _dataItem = value;

    public virtual object? DataItem
    {
        get => GetDataItem();
        set => SetDataItem(value);
    }

    public TableCell this[int index] => Cells[index];

    public TableCell this[string name]
    {
        get
        {
            var item = Grid.GetColumn(name);

            if (item == null)
            {
                throw new ArgumentOutOfRangeException(nameof(name), $"Column '{name}' not found.");
            }

            var index = Grid.GetColumIndex(item);

            return Cells[index];
        }
    }

    public virtual int ItemIndex { get; set; }

    public virtual Task DataBindAsync()
    {
        return Task.CompletedTask;
    }

    protected override Task AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        Grid.RowAttributes.AddAttributes(writer);
        return base.AddAttributesToRender(writer, token);
    }

    int IDataItemContainer.DataItemIndex => ItemIndex;

    int IDataItemContainer.DisplayIndex => ItemIndex;

    internal ValueTask AddCell(GridCell cell)
    {
        Cells.Add(cell);
        return Controls.AddAsync(cell);
    }

    internal async Task LoadEditItemTemplateAsync()
    {
        if (Grid.EditItemTemplate is null) return;

        EditItemTemplateContainer = new GridItemEditContainer(this);
        Grid.EditItemTemplate.InstantiateIn(EditItemTemplateContainer);
        await Controls.AddAsync(EditItemTemplateContainer);
    }
}


public class GridItemEditContainer : Control, INamingContainer
{
    protected override bool GenerateAutomaticID => false;

    public override string UniqueID => $"{Parent.UniqueID}{IdSeparator}_edit";

    public override string ClientID => $"{Parent.ClientID}_edit";

    private readonly GridItem _item;

    public GridItemEditContainer(GridItem item)
    {
        _item = item;
    }

    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);
        Visible = false;
    }

    public override async Task RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (!Visible) return;

        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Tr);

        _item.Grid.EditRowAttributes.AddAttributes(writer);
        writer.AddAttribute(HtmlTextWriterAttribute.Colspan, _item.Cells.Count(i => i.Visible).ToString());

        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Td);

        await base.RenderAsync(writer, token);

        await writer.RenderEndTagAsync();
        await writer.RenderEndTagAsync();
    }
}