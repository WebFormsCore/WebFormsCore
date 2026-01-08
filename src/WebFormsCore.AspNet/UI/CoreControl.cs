using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.HtmlControls;
using FxPageAsyncTask = System.Web.UI.PageAsyncTask;
using FxHttpWriter = System.Web.HttpWriter;
using FxHtmlTextWriterTag = System.Web.UI.HtmlTextWriterTag;
using FxHtmlTextWriterAttribute = System.Web.UI.HtmlTextWriterAttribute;

namespace WebFormsCore.UI;

/// <summary>
/// Wraps a <see cref="WebFormsCore.UI.Control"/> in a <see cref="System.Web.UI.Control"/> so that it can be used in a WebForms page.
/// </summary>
public class CoreControl : System.Web.UI.Control
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly IInternalControl _control;
    private readonly MemoryStream _memoryStream = new();
    private IViewStateManager _viewStateManager;

    private IViewStateManager ViewStateManager
    {
        get
        {
            InitializeControl();
            return _viewStateManager ??= _control.Context.RequestServices.GetRequiredService<IViewStateManager>();
        }
    }

    public CoreControl(IInternalControl control)
    {
        _control = control;
    }

    private void InitializeControl()
    {
        if (_control.IsInPage) return;

        // Make sure the control is added to a page so that it can be initialized.
        IInternalPage virtualPage = new Page();
        virtualPage.Initialize(Context.GetCoreContext());
        virtualPage.Control.Controls.AddWithoutPageEvents(_control.Control);
        _control.Page = virtualPage;
    }

    protected override void TrackViewState()
    {
        base.TrackViewState();

        _control.InvokeTrackViewState(CancellationToken.None);
    }

    private bool LoadViewState()
    {
        if (Context.Request.HttpMethod != "POST")
        {
            return false;
        }

        var viewState = Context.Request.Form[UniqueID];

        if (string.IsNullOrEmpty(viewState))
        {
            return false;
        }

        Page.RegisterAsyncTask(new FxPageAsyncTask(_ => ViewStateManager.LoadFromBase64Async(_control.Control, viewState).AsTask()));
        Page.ExecuteRegisteredAsyncTasks();

        return true;
    }

    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        InitializeControl();
        _control.FrameworkInit(CancellationToken.None);

        LoadViewState();

        Page.RegisterAsyncTask(new FxPageAsyncTask(token => _control.InitAsync(token).AsTask()));
        Page.ExecuteRegisteredAsyncTasks();

        if (LoadViewState())
        {
            var postbackTarget = Context.Request.Form["wfcTarget"];
            var postbackArgument = Context.Request.Form["wfcArgument"];

            Page.RegisterAsyncTask(new FxPageAsyncTask(token => _control.PostbackAsync(token, null, postbackTarget, postbackArgument).AsTask()));
            Page.ExecuteRegisteredAsyncTasks();
        }

        ScriptManager.RegisterStartupScript(Page, typeof(CoreControl), "WebFormsCore", HtmlBody.Script, true);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        Page.RegisterAsyncTask(new FxPageAsyncTask(token => _control.LoadAsync(token, null).AsTask()));
        Page.ExecuteRegisteredAsyncTasks();
    }

    protected override void OnPreRender(EventArgs e)
    {
        base.OnPreRender(e);

        Page.RegisterAsyncTask(new FxPageAsyncTask(async token =>
        {
            await _control.PreRenderAsync(token, null);

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

        writer.AddAttribute("data-wfc-form", "");
        writer.RenderBeginTag(FxHtmlTextWriterTag.Div);

        if (writer.InnerWriter is FxHttpWriter streamWriter)
        {
            writer.Flush();
            streamWriter.WriteBytes(buffer.Array!, buffer.Offset, buffer.Count);
            streamWriter.Flush();
        }
        else
        {
            writer.Write(Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count));
        }


        using var viewStateBuffer = ViewStateManager.WriteBase64(_control.Control, out var length);

        writer.AddAttribute(FxHtmlTextWriterAttribute.Type, "hidden");
        writer.AddAttribute(FxHtmlTextWriterAttribute.Name, UniqueID);
        writer.AddAttribute(FxHtmlTextWriterAttribute.Value, Encoding.UTF8.GetString(viewStateBuffer.Memory.Span.Slice(0, length)));
        writer.RenderBeginTag(FxHtmlTextWriterTag.Input);
        writer.RenderEndTag();

        writer.RenderEndTag();
    }
}
