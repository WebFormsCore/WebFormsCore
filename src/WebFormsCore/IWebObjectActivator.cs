using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public interface IWebObjectActivator
{
    T ParseAttribute<T>(string attributeValue);

    T ParseAttribute<T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TConverter>(string attributeValue) where TConverter : TypeConverter;

    T CreateControl<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        T>();

    object CreateControl(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type type);

    object CreateControl(string fullPath);

    LiteralControl CreateLiteral(string text);

    LiteralControl CreateLiteral(object? value);

    HtmlContainerControl CreateElement(string tagName);
}

public interface IPostBackAsyncLoadHandler
{
    /// <summary>
    /// Called after the control view state has been loaded but before the view state of child controls have been loaded.
    /// </summary>
    /// <remarks>
    /// This is directly after the method <see cref="Control.LoadViewState"/>.
    /// </remarks>
    Task AfterPostBackLoadAsync();
}

public interface IPostBackLoadHandler
{
    /// <summary>
    /// Called after the control view state has been loaded but before the view state of child controls have been loaded.
    /// </summary>
    /// <remarks>
    /// This is directly after the method <see cref="Control.LoadViewState"/>.
    /// </remarks>
    void AfterPostBackLoad();
}

public interface IBackgroundLoadHandler
{
    /// <summary>
    /// Loads the control in the background.
    /// </summary>
    /// <remarks>
    /// This is called after the control has been initialized and its view state has been loaded.
    /// </remarks>
    Task OnBackgroundLoadAsync();
}

public interface IPostBackEventHandler
{
    void RaisePostBackEvent(string? eventArgument);
}

public interface IPostBackAsyncEventHandler
{
    Task RaisePostBackEventAsync(string? eventArgument);
}

public interface IPostBackDataHandler
{
    bool LoadPostData(string postDataKey, IFormCollection postCollection);

    void RaisePostDataChangedEvent();
}

public interface IPostBackAsyncDataHandler
{
    ValueTask<bool> LoadPostDataAsync(string postDataKey, IFormCollection postCollection, CancellationToken cancellationToken);

    ValueTask RaisePostDataChangedEventAsync(CancellationToken cancellationToken);
}