using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlTitle : HtmlContainerControl
{
    public HtmlTitle()
        : base("title")
    {
    }

    protected override bool GenerateAutomaticID => false;

    public string Text
    {
        get => InnerText;
        set => InnerText = value;
    }
}
