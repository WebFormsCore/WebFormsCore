using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace WebFormsCore.UI.WebControls;

public static class LiteralMethods
{
    public static Literal Text(Func<string> textDelegate) => new(textDelegate);
    public static Literal Html(Func<string> htmlDelegate) => new(htmlDelegate) { Mode = LiteralMode.PassThrough };
    public static Literal Text(string text) => new() { Text = text };
    public static Literal Html(string html) => new() { Text = html, Mode = LiteralMode.PassThrough };
}

public partial class Literal : Control, ITextControl
{
    private Func<Task<string>>? _textDelegate;

    [ViewState] private LiteralMode? _mode;

    public Literal()
    {
    }

    public Literal(Func<Task<string>> textDelegate)
    {
        _textDelegate = textDelegate;
    }

    public Literal(Func<string> textDelegate)
    {
        _textDelegate = () => Task.FromResult(textDelegate());
    }

    public Literal(Func<Task<object>> textDelegate)
    {
        _textDelegate = async () => (await textDelegate())?.ToString() ?? string.Empty;
    }

    public Literal(Func<object> textDelegate)
    {
        _textDelegate = () => Task.FromResult(textDelegate()?.ToString() ?? string.Empty);
    }

    protected override bool EnableViewStateBag => false;

    [ViewState] public string? Text { get; set; }

    public LiteralMode Mode
    {
        get => _mode ??= Context.RequestServices.GetService<IOptions<WebFormsCoreOptions>>()?.Value.DefaultLiteralMode ?? LiteralMode.Encode;
        set => _mode = value;
    }

    public override async ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        var text = _textDelegate is null
            ? Text
            : await _textDelegate();

        if (string.IsNullOrEmpty(text))
            return;

        if (Mode == LiteralMode.Encode)
        {
            await writer.WriteEncodedTextAsync(text);
        }
        else
        {
            await writer.WriteAsync(text);
        }
    }

    string ITextControl.Text
    {
        get => Text ?? string.Empty;
        set => Text = value;
    }

    public override void ClearControl()
    {
        base.ClearControl();

        Text = null;
        Mode = LiteralMode.Transform;
        _textDelegate = null;
    }
}
