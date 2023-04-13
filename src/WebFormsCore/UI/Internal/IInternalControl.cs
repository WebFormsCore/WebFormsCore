﻿using System.Threading;
using System.Threading.Tasks;
using HttpStack;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI;

/// <summary>
/// Internal methods of <see cref="Control"/> are exposed through this interface to allow the control to be used in a WebForms page.
/// </summary>
public interface IInternalControl
{
    Control Control { get; }

    IHttpContext Context { get; }

    IInternalPage Page { get; set; }

    bool IsInPage { get; }

    void InvokeFrameworkInit(CancellationToken token);

    void InvokeTrackViewState(CancellationToken token);

    ValueTask InvokeInitAsync(CancellationToken token);

    ValueTask InvokePostbackAsync(CancellationToken token, HtmlForm? form, string? target, string? argument);

    ValueTask InvokeLoadAsync(CancellationToken token, HtmlForm? form);

    ValueTask InvokePreRenderAsync(CancellationToken token, HtmlForm? form);

    Task RenderAsync(HtmlTextWriter writer, CancellationToken token);
}
