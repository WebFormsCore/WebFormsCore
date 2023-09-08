using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            await RegisterCsp(token);
        }
    }

    private ValueTask RegisterCsp(CancellationToken token)
    {
        CspDirective directive;
        string? attributeName;

        if (_tagName.Equals("script", StringComparison.OrdinalIgnoreCase))
        {
            directive = Page.Csp.ScriptSrc;
            attributeName = "src";
        }
        else if (_tagName.Equals("link", StringComparison.OrdinalIgnoreCase) && Attributes["rel"] == "stylesheet")
        {
            directive = Page.Csp.StyleSrc;
            attributeName = "href";
        }
        else if (_tagName.Equals("style", StringComparison.OrdinalIgnoreCase))
        {
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

        return RegisterCsp(attributeName, directive, token);
    }

    private async ValueTask RegisterCsp(string? attributeName, CspDirective directive, CancellationToken token)
    {
        if (attributeName is not null)
        {
            var value = Attributes[attributeName];

            if (value != null && value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                directive.Add("data:");
                return;
            }

            if (Uri.TryCreate(value, UriKind.Absolute, out var href))
            {
                directive.Add($"{href.Scheme}://{href.Host}");
                return;
            }
        }

        if (directive is CspDirectiveGenerated extended)
        {
            if (extended.Mode == CspMode.Sha256)
            {
                _preRenderedContent = await this.RenderChildrenToStringAsync(token);
                extended.AddInlineHash(_preRenderedContent);
            }
            else
            {
                _nonce = extended.GenerateNonce();
            }
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
