namespace WebFormsCore.UI.WebControls;

public partial class LinkButton : Button
{
    public LinkButton()
        : base(HtmlTextWriterTag.A)
    {
    }

    protected override void AddButtonAttributesToRender(HtmlTextWriter writer)
    {
        if (TagKey is HtmlTextWriterTag.A && !Attributes.ContainsKey("href"))
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
        }

        writer.AddAttribute("role", "button");
    }
}
