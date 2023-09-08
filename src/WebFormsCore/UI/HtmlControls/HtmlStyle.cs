namespace WebFormsCore.UI.HtmlControls;

public class HtmlStyle : HtmlGenericControl
{
    protected override bool GenerateAutomaticID => false;

    public HtmlStyle()
        : base("style")
    {
    }
}
