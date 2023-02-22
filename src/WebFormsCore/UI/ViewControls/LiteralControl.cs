// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI;

/// <summary>Represents HTML elements, text, and any other strings in an ASP.NET page that do not require processing on the server.</summary>
[ToolboxItem(false)]
public sealed class LiteralControl : Control, ITextControl
{
    private string _text;

    public LiteralControl()
    {
        _text = string.Empty;
    }

    public override bool EnableViewState
    {
        get => false;
        set
        {
            if (value)
            {
                throw new InvalidOperationException("Cannot set EnableViewState to true for a LiteralControl.");
            }
        }
    }

    /// <summary>Gets or sets the text content of the <see cref="T:WebFormsCore.UI.LiteralControl" /> object.</summary>
    /// <returns>A <see cref="T:System.String" /> that represents the text content of the literal control. The default is <see cref="F:System.String.Empty" />.</returns>
    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    /// <summary>Creates an <see cref="T:WebFormsCore.UI.EmptyControlCollection" /> object for the current instance of the <see cref="T:WebFormsCore.UI.LiteralControl" /> class.</summary>
    /// <returns>The <see cref="T:WebFormsCore.UI.EmptyControlCollection" /> for the current control.</returns>
    protected override ControlCollection CreateControlCollection() => new EmptyControlCollection(this);

    /// <summary>Writes the content of the <see cref="T:WebFormsCore.UI.LiteralControl" /> object to the ASP.NET page.</summary>
    /// <param name="output">An <see cref="T:WebFormsCore.UI.HtmlTextWriter" /> that renders the content of the <see cref="T:WebFormsCore.UI.LiteralControl" /> to the requesting client. </param>
    /// <param name="token"></param>
    public override Task RenderAsync(HtmlTextWriter output, CancellationToken token) => output.WriteAsync(_text);

    public override void ClearControl()
    {
        base.ClearControl();
        _text = string.Empty;
    }

    protected override void OnWriteViewState(ref ViewStateWriter writer)
    {
    }

    protected override void OnLoadViewState(ref ViewStateReader reader)
    {
    }
}
