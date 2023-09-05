using System;
using System.Threading;
using System.Threading.Tasks;
using HttpStack.Collections;

namespace WebFormsCore.UI.WebControls;

public partial class CheckBox : WebControl, IPostBackAsyncDataHandler
{
    public CheckBox()
        : base(HtmlTextWriterTag.Input)
    {
    }

    public override bool SupportsDisabledAttribute => true;

    [ViewState] public bool AutoPostBack { get; set; }

    [ViewState] public bool Checked { get; set; }

    public event AsyncEventHandler? CheckedChanged;

    protected override async Task AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        await base.AddAttributesToRender(writer, token);

        writer.AddAttribute(HtmlTextWriterAttribute.Type, "checkbox");
        writer.AddAttribute(HtmlTextWriterAttribute.Name, UniqueID);

        if (Checked)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Checked, "checked");
        }

        if (AutoPostBack) writer.AddAttribute("data-wfc-autopostback", null);
    }

    public override async Task RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        await AddAttributesToRender(writer, token);
        await writer.RenderSelfClosingTagAsync(TagKey);
    }

    protected virtual ValueTask<bool> LoadPostDataAsync(string postDataKey, IFormCollection postCollection, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            return new ValueTask<bool>(false);
        }

        var value = postCollection.TryGetValue(postDataKey, out var formValue) && !string.IsNullOrEmpty(formValue);
        var isChanged = value != Checked;
        Checked = value;

        return new ValueTask<bool>(CheckedChanged != null && isChanged);
    }

    protected virtual ValueTask RaisePostDataChangedEventAsync(CancellationToken cancellationToken)
    {
        if (CheckedChanged != null)
        {
            return CheckedChanged.InvokeAsync(this, EventArgs.Empty);
        }

        return default;
    }

    ValueTask<bool> IPostBackAsyncDataHandler.LoadPostDataAsync(string postDataKey, IFormCollection postCollection, CancellationToken cancellationToken)
        => LoadPostDataAsync(postDataKey, postCollection, cancellationToken);

    ValueTask IPostBackAsyncDataHandler.RaisePostDataChangedEventAsync(CancellationToken cancellationToken)
        => RaisePostDataChangedEventAsync(cancellationToken);
}
