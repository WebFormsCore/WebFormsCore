using System;
using System.ComponentModel;
using System.Threading.Tasks;
using WebFormsCore.UI;
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

    HtmlContainerControl CreateElement(string tagName);
}

public interface IPostBackLoadHandler
{
    /// <summary>
    /// Called after the control view state has been loaded but before the view state of child controls have been loaded.
    /// </summary>
    /// <remarks>
    /// This is directly after the method <see cref="Control.LoadViewState"/>.
    /// </remarks>
    Task AfterPostBackLoadAsync();
}

public interface IPostBackEventHandler
{
    void RaisePostBackEvent(string? eventArgument);
}

public interface IPostBackAsyncEventHandler
{
    Task RaisePostBackEventAsync(string? eventArgument);
}
