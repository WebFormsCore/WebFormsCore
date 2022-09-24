using System;
using System.Threading.Tasks;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public interface IWebObjectActivator
{
    T CreateControl<T>();

    object CreateControl(Type type);

    LiteralControl CreateLiteral(string text);

    LiteralControl CreateLiteral(object? value);

    HtmlGenericControl CreateHtml(string tagName);
}

public interface IPostBackEventHandler
{
    ValueTask RaisePostBackEventAsync();
}
