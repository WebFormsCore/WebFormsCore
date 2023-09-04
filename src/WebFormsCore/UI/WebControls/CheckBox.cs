using System.Threading;
using System.Threading.Tasks;

namespace WebFormsCore.UI.WebControls;

public partial class CheckBox : WebControl
{
    public CheckBox()
        : base(HtmlTextWriterTag.Input)
    {
    }

    [ViewState] public bool Checked { get; set; }

    protected override async Task AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        await base.AddAttributesToRender(writer, token);

        writer.AddAttribute(HtmlTextWriterAttribute.Type, "checkbox");

        if (Checked)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Checked, "checked");
        }
    }

    public override async Task RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        await AddAttributesToRender(writer, token);
        await writer.RenderSelfClosingTagAsync(TagKey);
    }
}
