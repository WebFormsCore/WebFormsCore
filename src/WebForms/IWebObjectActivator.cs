using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace System.Web;

public interface IWebObjectActivator
{
    T CreateControl<T>();

    object CreateControl(Type type);

    LiteralControl CreateLiteral(string text);

    LiteralControl CreateLiteral(object? value);

    HtmlGenericControl CreateHtml(string tagName);
}