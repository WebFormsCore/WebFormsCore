using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.UI;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.HtmlControls;
using FxPageAsyncTask = System.Web.UI.PageAsyncTask;
using FxHttpWriter = System.Web.HttpWriter;

namespace WebFormsCore.UI;

/// <summary>
/// Wraps a <see cref="WebFormsCore.UI.Control"/> in a <see cref="System.Web.UI.Control"/> so that it can be used in a WebForms page.
/// </summary>
public class CoreControl : System.Web.UI.TemplateControl
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly IInternalControl _control;
    private readonly MemoryStream _memoryStream = new();
    private IViewStateManager _viewStateManager;

    private IViewStateManager ViewStateManager => _viewStateManager ??= _control.Context.RequestServices.GetRequiredService<IViewStateManager>();

    public CoreControl(IInternalControl control)
    {
        _control = control;
    }

    protected override void FrameworkInitialize()
    {
        base.FrameworkInitialize();

        // Make sure the control is added to a page so that it can be initialized.
        if (!_control.IsInPage)
        {
            IInternalPage virtualPage = new Page();
            virtualPage.Initialize(_control.Context);
            virtualPage.Control.Controls.AddWithoutPageEvents(_control.Control);
            _control.Page = virtualPage;
        }

        _control.InvokeFrameworkInit(CancellationToken.None);
    }

    protected override void LoadViewState(object savedState)
    {
        if (savedState is not string state)
        {
            return;
        }

        Page.RegisterAsyncTask(new FxPageAsyncTask(_ => ViewStateManager.LoadFromBase64Async(_control.Control, state).AsTask()));
        Page.ExecuteRegisteredAsyncTasks();
    }

    protected override object SaveViewState()
    {
        using var buffer = ViewStateManager.Write(_control.Control, out var length);
        return Encoding.UTF8.GetString(buffer.Memory.Span.Slice(0, length));
    }

    protected override void TrackViewState()
    {
        base.TrackViewState();

        _control.InvokeTrackViewState(CancellationToken.None);
    }

    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        ScriptManager.RegisterStartupScript(Page, typeof(CoreControl), "WebFormsCore", HtmlBody.Script, true);

        Page.RegisterAsyncTask(new FxPageAsyncTask(token => _control.InvokeInitAsync(token).AsTask()));
        Page.ExecuteRegisteredAsyncTasks();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        Page.RegisterAsyncTask(new FxPageAsyncTask(token => _control.InvokeLoadAsync(token, null).AsTask()));
        Page.ExecuteRegisteredAsyncTasks();
    }

    protected override void OnPreRender(EventArgs e)
    {
        base.OnPreRender(e);

        Page.RegisterAsyncTask(new FxPageAsyncTask(async token =>
        {
            await _control.InvokePreRenderAsync(token, null);

            using var writer = new StreamWriter(_memoryStream, Utf8WithoutBom, 1024, true);
            var innerWriter = new HtmlTextWriter(writer);

            await _control.RenderAsync(innerWriter, token);
        }));

        Page.ExecuteRegisteredAsyncTasks();
    }

    protected override void Render(System.Web.UI.HtmlTextWriter writer)
    {
        base.Render(writer);

        if (!_memoryStream.TryGetBuffer(out var buffer) || buffer.Array is null)
        {
            var array = _memoryStream.ToArray();
            buffer = new ArraySegment<byte>(array, 0, array.Length);
        }

        if (writer.InnerWriter is FxHttpWriter streamWriter)
        {
            streamWriter.WriteBytes(buffer.Array!, buffer.Offset, buffer.Count);
        }
        else
        {
            writer.Write(Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count));
        }
    }
}
