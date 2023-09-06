using WebFormsCore.UI.Attributes;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI;

public class CellEventArgs : EventArgs
{
    public CellEventArgs(GridCell cell, GridItem item)
    {
        Cell = cell;
        Item = item;
    }

    public GridCell Cell { get; }

    public GridItem Item { get; }
}

[ParseChildren(true)]
public abstract partial class GridColumn : WebControl
{
    protected override bool GenerateAutomaticID => false;

    [ViewState] private string? _uniqueName;
    [ViewState] public string? HeaderText { get; set; }
    [ViewState] public AttributeCollection CellAttributes { get; set; } = new();

    public event AsyncEventHandler<GridColumn, CellEventArgs>? CellCreated;

    public event AsyncEventHandler<GridColumn, CellEventArgs>? CellDataBound;

    public GridColumn()
        : base(HtmlTextWriterTag.Th)
    {
    }

    public ITemplate? HeaderTemplate { get; set; }

    public string? UniqueName
    {
        get => _uniqueName ?? GetDefaultUniqueName();
        set => _uniqueName = value;
    }

    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);
        Controls.Clear();
        HeaderTemplate?.InstantiateIn(this);
    }

    protected override Task RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return HasRenderingData()
            ? base.RenderContentsAsync(writer, token)
            : writer.WriteAsync(HeaderText);
    }

    public virtual GridCell CreateCell(Page page, GridItem item)
    {
        return page.WebActivator.CreateControl<GridCell>();
    }

    public virtual async ValueTask InvokeDataBinding(GridCell cell, GridItem item)
    {
        if (CellDataBound != null)
        {
            await CellDataBound.InvokeAsync(this, new CellEventArgs(cell, item));
        }
    }

    public virtual async ValueTask InvokeItemCreated(GridCell cell, GridItem item)
    {
        if (CellCreated != null)
        {
            await CellCreated.InvokeAsync(this, new CellEventArgs(cell, item));
        }
    }

    protected virtual string? GetDefaultUniqueName()
    {
        return null;
    }
}