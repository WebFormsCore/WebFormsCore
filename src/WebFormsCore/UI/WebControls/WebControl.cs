using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace WebFormsCore.UI.WebControls;

public partial class WebControl : Control, IAttributeAccessor
{
    [ViewState] private AttributeCollection _attributes = new();

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.WebControls.WebControl" /> class that represents a <see langword="Span" /> HTML tag.</summary>
    protected WebControl()
        : this(HtmlTextWriterTag.Span)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.WebControls.WebControl" /> class using the specified HTML tag.</summary>
    /// <param name="tag">One of the <see cref="T:System.Web.UI.HtmlTextWriterTag" /> values. </param>
    public WebControl(HtmlTextWriterTag tag) => TagKey = tag;

    public string? CssClass
    {
        get => Attributes.CssClass;
        set => Attributes.CssClass = value;
    }

    protected virtual HtmlTextWriterTag TagKey { get; }

    public virtual string? DisabledCssClass => Context.RequestServices.GetService<IOptions<WebFormsCoreOptions>>()?.Value?.DisabledClass;

    public virtual bool SupportsDisabledAttribute => TagKey is (
        HtmlTextWriterTag.Button or
        HtmlTextWriterTag.Input or
        HtmlTextWriterTag.Select or
        HtmlTextWriterTag.Textarea or
        HtmlTextWriterTag.Option or
        HtmlTextWriterTag.Fieldset or
        HtmlTextWriterTag.Menu);

    protected virtual string TagName => TagKey == HtmlTextWriterTag.Unknown ? "span" : TagKey.ToName();

    [ViewState] public virtual bool Enabled { get; set; } = true;

    [ViewState] public string? ToolTip { get; set; }

    [ViewState] public short TabIndex { get; set; }

    protected bool IsEnabled
    {
        get
        {
            if (!Visible)
            {
                return false;
            }

            for (Control? control = this; control != null; control = control.ParentInternal)
            {
                if (control is WebControl { Enabled: false })
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>Gets a collection of all attribute name and value pairs expressed on a server control tag within the ASP.NET page.</summary>
    /// <returns>A <see cref="T:WebFormsCore.UI.AttributeCollection" /> object that contains all attribute name and value pairs expressed on a server control tag within the Web page.</returns>
    public AttributeCollection Attributes => _attributes;

    public CssStyleCollection Style => _attributes.CssStyle;

    protected virtual ValueTask AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        if (AddClientIdToAttributes && ClientID is {} clientId)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Id, clientId);
        }

        if (TabIndex > 0)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Tabindex, TabIndex.ToString(NumberFormatInfo.InvariantInfo));
        }

        if (!string.IsNullOrEmpty(ToolTip))
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Title, ToolTip);
        }

        _attributes.AddAttributes(writer);

        if (!_attributes.ContainsKey("class") && IsInPage)
        {
            ServiceProvider.GetService<IControlHtmlClassProvider>()?.WriteDefaultClass(this, writer);
        }

        if (!IsEnabled)
        {
            if (SupportsDisabledAttribute)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled");
            }
            else
            {
                writer.AddAttribute("aria-disabled", "true");
            }

            if (!string.IsNullOrEmpty(DisabledCssClass))
            {
                writer.MergeAttribute(HtmlTextWriterAttribute.Class, DisabledCssClass);
            }

            writer.AddAttribute("data-wfc-disabled", "true");
        }

        return default;
    }

    public async ValueTask RenderBeginTag(HtmlTextWriter writer, CancellationToken token)
    {
        await AddAttributesToRender(writer, token);

        var tagKey = TagKey;
        if (tagKey != HtmlTextWriterTag.Unknown)
        {
            await writer.RenderBeginTagAsync(tagKey);
        }
        else
        {
            await writer.RenderBeginTagAsync(TagName);
        }
    }

    public virtual ValueTask RenderEndTagAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return writer.RenderEndTagAsync();
    }

    protected virtual ValueTask RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return base.RenderAsync(writer, token);
    }

    public override async ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (!Visible)
        {
            return;
        }

        var isVoidTag = Control.IsVoidTag(TagName);

        if (isVoidTag && !HasControls())
        {
            await AddAttributesToRender(writer, token);
            await writer.RenderSelfClosingTagAsync(TagName);
        }
        else
        {
            await RenderBeginTag(writer, token);
            await RenderContentsAsync(writer, token);
            await RenderEndTagAsync(writer, token);
        }
    }

    public override void ClearControl()
    {
        base.ClearControl();

        _attributes.Clear();
        Enabled = true;
        ToolTip = null;
        TabIndex = 0;
    }

    protected virtual string? GetAttribute(string name) => _attributes[name];

    protected virtual void SetAttribute(string name, string? value) => _attributes[name] = value;

    /// <inheritdoc />
    string? IAttributeAccessor.GetAttribute(string name) => GetAttribute(name);

    /// <inheritdoc />
    void IAttributeAccessor.SetAttribute(string name, string? value) => SetAttribute(name, value);
}
