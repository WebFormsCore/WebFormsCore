using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.HtmlControls;

public class HtmlScript : HtmlGenericControl
{
    protected override bool GenerateAutomaticID => false;

    public HtmlScript()
        : base("script")
    {
    }
}
