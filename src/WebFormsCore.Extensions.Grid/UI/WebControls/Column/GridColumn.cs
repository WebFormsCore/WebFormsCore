using WebFormsCore.UI.Attributes;

namespace WebFormsCore.UI.WebControls;

public class CellEventArgs : EventArgs
{
    public CellEventArgs(GridCell cell, GridItem item, bool isPostBack)
    {
        Cell = cell;
        Item = item;
        IsPostBack = isPostBack;
    }

    public GridCell Cell { get; }

    public GridItem Item { get; }

    public bool IsPostBack { get; }
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

    protected virtual string? GetHeaderText()
    {
        return HeaderText;
    }

    protected override ValueTask RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return HasRenderingData()
            ? base.RenderContentsAsync(writer, token)
            : writer.WriteAsync(GetHeaderText());
    }

    public virtual GridCell CreateCell(Page page, GridItem item)
    {
        return new GridCell();
    }

    public virtual async ValueTask InvokeDataBinding(GridCell cell, GridItem item, bool isPostBack)
    {
        if (CellDataBound != null)
        {
            await CellDataBound.InvokeAsync(this, new CellEventArgs(cell, item, isPostBack));
        }
    }

    public virtual async ValueTask InvokeItemCreated(GridCell cell, GridItem item, bool isPostBack)
    {
        if (CellCreated != null)
        {
            await CellCreated.InvokeAsync(this, new CellEventArgs(cell, item, isPostBack));
        }
    }

    protected virtual string? GetDefaultUniqueName()
    {
        return null;
    }
}