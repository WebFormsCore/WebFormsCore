using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI;

public interface IControlHtmlClassProvider
{
    void WriteDefaultClass(WebControl control, HtmlTextWriter writer);
}
