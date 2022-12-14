using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls;

public partial class WebControl : Control, IAttributeAccessor
{
    private AttributeCollection? _attributes;
    private string? _tagName;

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.WebControls.WebControl" /> class that represents a <see langword="Span" /> HTML tag.</summary>
    protected WebControl()
        : this(HtmlTextWriterTag.Span)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.UI.WebControls.WebControl" /> class using the specified HTML tag.</summary>
    /// <param name="tag">One of the <see cref="T:System.Web.UI.HtmlTextWriterTag" /> values. </param>
    public WebControl(HtmlTextWriterTag tag) => TagKey = tag;

    protected virtual HtmlTextWriterTag TagKey { get; }

    public virtual bool SupportsDisabledAttribute => true;

    protected virtual string TagName
    {
        get
        {
            if (_tagName == null && TagKey != HtmlTextWriterTag.Unknown)
            {
                _tagName = Enum.Format(typeof (HtmlTextWriterTag), TagKey, "G").ToLower(CultureInfo.InvariantCulture);
            }

            return _tagName ?? "span";
        }
    }

    [ViewState] public bool Enabled { get; set; } = true;

    [ViewState] public string? ToolTip { get; set; }

    [ViewState] public short TabIndex { get; set; }

    protected bool IsEnabled => true; // TODO: Implement this property

    /// <summary>Gets a collection of all attribute name and value pairs expressed on a server control tag within the ASP.NET page.</summary>
    /// <returns>A <see cref="T:WebFormsCore.UI.AttributeCollection" /> object that contains all attribute name and value pairs expressed on a server control tag within the Web page.</returns>
    public AttributeCollection Attributes => _attributes ??= new AttributeCollection(ViewState);

    protected virtual Task AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        if (ID != null)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Id, ClientID);
        }

        if (!Enabled)
        {
            if (SupportsDisabledAttribute)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled");
            }
        }

        if (TabIndex > 0)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Tabindex, TabIndex.ToString(NumberFormatInfo.InvariantInfo));
        }

        if (!string.IsNullOrEmpty(ToolTip))
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Title, ToolTip);
        }

        _attributes?.AddAttributes(writer);

        return Task.CompletedTask;
    }

    public async Task RenderBeginTag(HtmlTextWriter writer, CancellationToken token)
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

    public virtual async Task RenderEndTagAsync(HtmlTextWriter writer, CancellationToken token)
    {
        await writer.RenderEndTagAsync();
    }

    protected virtual Task RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return base.RenderAsync(writer, token);
    }

    public override async Task RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        await RenderBeginTag(writer, token);
        await RenderContentsAsync(writer, token);
        await RenderEndTagAsync(writer, token);
    }

    protected virtual string? GetAttribute(string name) => _attributes?[name];

    protected virtual void SetAttribute(string name, string? value) => Attributes[name] = value;

    /// <inheritdoc />
    string? IAttributeAccessor.GetAttribute(string name) => GetAttribute(name);

    /// <inheritdoc />
    void IAttributeAccessor.SetAttribute(string name, string value) => SetAttribute(name, value);
}
