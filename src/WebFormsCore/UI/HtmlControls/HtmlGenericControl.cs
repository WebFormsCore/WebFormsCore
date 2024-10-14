using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebFormsCore.Security;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

/// <summary>Defines the methods, properties, and events for all HTML server control elements not represented by a specific .NET Framework class.</summary>
public class HtmlGenericControl : HtmlContainerControl
{
    private string? _preRenderedContent;
    private string? _nonce;

    /// <summary>Initializes a new instance of the <see cref="T:WebFormsCore.UI.HtmlControls.HtmlGenericControl" /> class with default values.</summary>
    public HtmlGenericControl()
        : this("span")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="T:WebFormsCore.UI.HtmlControls.HtmlGenericControl" /> class with the specified tag.</summary>
    /// <param name="tag">The name of the element for which this instance of the class is created. </param>
    public HtmlGenericControl(string tag)
        : base(tag)
    {
    }

    /// <summary>Gets or sets the name of the HTML element represented by the <see cref="T:WebFormsCore.UI.HtmlControls.HtmlGenericControl" /> control.</summary>
    /// <returns>The tag name of an element.</returns>
    [DefaultValue("")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new string TagName
    {
        get => _tagName;
        set => _tagName = value;
    }

    protected override async ValueTask OnPreRenderAsync(CancellationToken token)
    {
        await base.OnPreRenderAsync(token);

        if (Page.Csp.Enabled)
        {
            await RegisterCspAsync(token);
        }
    }

    private ValueTask RegisterCspAsync(CancellationToken token)
    {
        CspDirective directive;
        string? attributeName;

        if (_tagName.Equals("script", StringComparison.OrdinalIgnoreCase))
        {
            if (!HtmlScript.RenderScripts(Page))
            {
                return default;
            }

            directive = Page.Csp.ScriptSrc;
            attributeName = "src";
        }
        else if (_tagName.Equals("link", StringComparison.OrdinalIgnoreCase) && Attributes["rel"] == "stylesheet")
        {
            if (!HtmlStyle.RenderStyles(Page))
            {
                return default;
            }

            directive = Page.Csp.StyleSrc;
            attributeName = "href";
        }
        else if (_tagName.Equals("style", StringComparison.OrdinalIgnoreCase))
        {
            if (!HtmlStyle.RenderStyles(Page))
            {
                return default;
            }

            directive = Page.Csp.StyleSrc;
            attributeName = null;
        }
        else if (_tagName.Equals("img", StringComparison.OrdinalIgnoreCase))
        {
            directive = Page.Csp.ImgSrc;
            attributeName = "src";
        }
        else
        {
            return default;
        }

        return RegisterCspAsync(attributeName, directive, token);
    }

    private async ValueTask RegisterCspAsync(string? attributeName, CspDirective directive, CancellationToken token)
    {
        if (attributeName is not null &&
            (directive is not CspDirectiveGenerated { Mode: var mode } || mode.HasFlag(CspMode.Uri)) &&
            directive.TryAddUri(Attributes[attributeName]))
        {
            return;
        }

        if (directive is CspDirectiveGenerated extended)
        {
            if (extended.Mode.HasFlag(CspMode.Sha256))
            {
                var preRenderedContent = await this.RenderChildrenToStringAsync(token);

                if (!string.IsNullOrWhiteSpace(preRenderedContent))
                {
                    _preRenderedContent = preRenderedContent;
                    extended.AddInlineHash(preRenderedContent);
                    return;
                }
            }

            _nonce = extended.GenerateNonce();
        }
    }

    protected override async ValueTask RenderAttributesAsync(HtmlTextWriter writer)
    {
        await base.RenderAttributesAsync(writer);

        if (_nonce != null) await writer.WriteAttributeAsync("nonce", _nonce);
    }

    protected override ValueTask RenderChildrenAsync(HtmlTextWriter writer, CancellationToken token)
    {
        return _preRenderedContent != null
            ? writer.WriteAsync(_preRenderedContent)
            : base.RenderChildrenAsync(writer, token);
    }

    public override void ClearControl()
    {
        base.ClearControl();
        _preRenderedContent = null;
        _nonce = null;
    }
}
