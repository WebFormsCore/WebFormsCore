namespace WebFormsCore.UI.HtmlControls;

public class HtmlLink : HtmlGenericControl
{
    protected override bool GenerateAutomaticID => false;

    public HtmlLink()
        : base("link")
    {
    }
}
