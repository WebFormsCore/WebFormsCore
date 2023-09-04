using WebFormsCore.UI.Attributes;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI;

[ParseChildren(true)]
public abstract partial class GridColumn : WebControl
{
    protected override bool GenerateAutomaticID => false;

    [ViewState] private string? _uniqueName;
    [ViewState] public string? HeaderText { get; set; }

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

    public virtual ValueTask InvokeDataBinding(GridCell cell, GridItem item)
    {
        return default;
    }

    public virtual ValueTask InvokeItemCreated(GridCell cell, GridItem item)
    {
        return default;
    }

    protected virtual string? GetDefaultUniqueName()
    {
        return null;
    }
}