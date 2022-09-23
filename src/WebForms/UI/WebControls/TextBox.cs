using System;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI.WebControls;

public class TextBox : HtmlControl
{
    public TextBox()
        : base("input")
    {
    }

    public string? Text { get; set; }

    public bool AutoPostBack { get; set; }

    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);

        if (Context.Request.HttpMethod == "POST")
        {
            Text = Context.Request.Form[ClientID];
        }
    }

    protected override async ValueTask RenderAttributesAsync(HtmlTextWriter writer)
    {
        await base.RenderAttributesAsync(writer);
        await writer.WriteAttributeAsync("name", ClientID);
        await writer.WriteAttributeAsync("value", Text);
        if (AutoPostBack) await writer.WriteAttributeAsync("data-wfc-autopostback", null);
    }

    protected override void SetAttribute(string name, string value)
    {
        if (name.Equals("name", StringComparison.OrdinalIgnoreCase))
        {
            // ignore
        }
        else if (name.Equals("value", StringComparison.OrdinalIgnoreCase))
        {
            Text = value;
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }
}
