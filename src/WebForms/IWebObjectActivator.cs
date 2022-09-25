using System;
using System.ComponentModel;
using System.Threading.Tasks;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public interface IWebObjectActivator
{
    T ParseAttribute<T>(string attributeValue);

    T ParseAttribute<T, TConverter>(string attributeValue) where TConverter : TypeConverter;

    T CreateControl<T>();

    object CreateControl(Type type);

    LiteralControl CreateLiteral(string text);

    LiteralControl CreateLiteral(object? value);

    HtmlGenericControl CreateElement(string tagName);
}

public interface IPostBackEventHandler
{
    ValueTask RaisePostBackEventAsync();
}
