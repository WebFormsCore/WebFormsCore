using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.UI;

namespace WebFormsCore.UI;

public class CoreControl : System.Web.UI.TemplateControl
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly IInternalControl _control;
    private readonly MemoryStream _memoryStream = new MemoryStream();

    public CoreControl(IInternalControl control)
    {
        _control = control;
    }

    protected override void FrameworkInitialize()
    {
        base.FrameworkInitialize();

        _control.InvokeFrameworkInit(CancellationToken.None);
    }

    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        Page.RegisterAsyncTask(new PageAsyncTask(token => _control.InvokeInitAsync(token).AsTask()));
        Page.ExecuteRegisteredAsyncTasks();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        Page.RegisterAsyncTask(new PageAsyncTask(token => _control.InvokeLoadAsync(token, null).AsTask()));
        Page.ExecuteRegisteredAsyncTasks();
    }

    protected override void OnPreRender(EventArgs e)
    {
        base.OnPreRender(e);

        Page.RegisterAsyncTask(new PageAsyncTask(async token =>
        {
            await _control.InvokePreRenderAsync(token, null);

            using var writer = new StreamWriter(_memoryStream, Utf8WithoutBom, 1024, true);
            var innerWriter = new WebFormsCore.UI.HtmlTextWriter(writer);

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

        if (writer.InnerWriter is HttpWriter streamWriter)
        {
            streamWriter.WriteBytes(buffer.Array!, buffer.Offset, buffer.Count);
        }
        else
        {
            writer.Write(Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count));
        }
    }
}
