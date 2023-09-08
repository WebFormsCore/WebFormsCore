namespace WebFormsCore.UI.HtmlControls;

public class HtmlImage : HtmlGenericControl
{
    protected override bool GenerateAutomaticID => false;

    public HtmlImage()
        : base("img")
    {
    }
}
