using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI.WebControls;

public partial class Button : WebControl, IPostBackAsyncEventHandler
{
    public Button()
        : base(HtmlTextWriterTag.Button)
    {
    }

    public event AsyncEventHandler<Button, EventArgs>? Click;

    [ViewState] public AttributeCollection Style { get; set; } = new();

    [ViewState]
    public string? Text
    {
        get => Controls.Count == 1 && Controls[0] is LiteralControl literal ? literal.Text : null;
        set
        {
            if (Controls.Count == 1 && Controls[0] is LiteralControl literal)
            {
                literal.Text = value ?? string.Empty;
            }
            else
            {
                Controls.Clear();

                if (value != null)
                {
                    Controls.AddWithoutPageEvents(
                        IsInPage ? WebActivator.CreateLiteral(value) : new LiteralControl { Text = value }
                    );
                }
            }
        }
    }

    public async Task RaisePostBackEventAsync(string? eventArgument)
    {
        await Click.InvokeAsync(this, EventArgs.Empty);
    }

    protected override async Task AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        await base.AddAttributesToRender(writer, token);

        if (TagKey is HtmlTextWriterTag.Button or HtmlTextWriterTag.Input)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "button");
        }

        writer.AddAttribute("data-wfc-postback", UniqueID);
    }
}
