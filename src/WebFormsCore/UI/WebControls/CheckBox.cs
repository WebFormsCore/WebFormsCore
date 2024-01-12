using System;
using System.Threading;
using System.Threading.Tasks;
using HttpStack.Collections;

namespace WebFormsCore.UI.WebControls;

public partial class CheckBox : WebControl, IPostBackAsyncDataHandler
{
    private string? _validationGroup;

    public CheckBox()
        : base(HtmlTextWriterTag.Input)
    {
    }

    protected override bool AddClientIdToAttributes => base.AddClientIdToAttributes || !string.IsNullOrEmpty(Text);

    public override bool SupportsDisabledAttribute => true;

    [ViewState] public bool AutoPostBack { get; set; }

    [ViewState] public bool Checked { get; set; }

    [ViewState] public bool CausesValidation { get; set; }

    [ViewState] public string Text { get; set; } = "";

    public AttributeCollection InputAttributes => Attributes;

    [ViewState] public AttributeCollection LabelAttributes { get; set; } = new();

    public string ValidationGroup
    {
        get => _validationGroup ?? Page.GetDefaultValidationGroup(this);
        set => _validationGroup = value;
    }

    public event AsyncEventHandler<CheckBox, EventArgs>? CheckedChanged;

    protected override async ValueTask AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        await base.AddAttributesToRender(writer, token);

        writer.AddAttribute(HtmlTextWriterAttribute.Type, "checkbox");
        writer.AddAttribute(HtmlTextWriterAttribute.Name, UniqueID);

        if (Checked)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Checked, "checked");
        }

        if (AutoPostBack)
        {
            writer.AddAttribute("data-wfc-autopostback", null);
        }

        if (CausesValidation)
        {
            writer.AddAttribute("data-wfc-validate", ValidationGroup);
        }
    }

    public override async ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        await AddAttributesToRender(writer, token);
        await writer.RenderSelfClosingTagAsync(TagKey);

        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        writer.AddAttribute(HtmlTextWriterAttribute.For, ClientID);
        LabelAttributes.AddAttributes(writer);
        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Label);
        await writer.WriteAsync(Text);
        await writer.RenderEndTagAsync();
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

    public override void ClearControl()
    {
        base.ClearControl();

        AutoPostBack = false;
        Checked = false;
        CheckedChanged = null;
    }

    ValueTask<bool> IPostBackAsyncDataHandler.LoadPostDataAsync(string postDataKey, IFormCollection postCollection, CancellationToken cancellationToken)
        => LoadPostDataAsync(postDataKey, postCollection, cancellationToken);

    ValueTask IPostBackAsyncDataHandler.RaisePostDataChangedEventAsync(CancellationToken cancellationToken)
        => RaisePostDataChangedEventAsync(cancellationToken);
}
