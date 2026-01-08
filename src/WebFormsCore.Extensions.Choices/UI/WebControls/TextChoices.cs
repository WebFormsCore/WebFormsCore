using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls.Internal;

namespace WebFormsCore.UI.WebControls;

public partial class TextChoices : ChoicesBase, IPostBackAsyncDataHandler, IPostBackAsyncEventHandler, IValidateableControl
{
    private readonly IOptions<WebFormsCoreOptions>? _options;

    [ViewState(nameof(SaveTextViewState))] private string? _json;

    [ViewState] public bool IsReadOnly { get; set; }

    [ViewState] public bool AutoPostBack { get; set; }

    protected virtual bool SaveTextViewState => (ValuesChanged != null || IsReadOnly);

    public event AsyncEventHandler<TextChoices, EventArgs>? ValuesChanged;

    private readonly ILogger<TextChoices>? _logger;
    private ICollection<string>? _values;
    private bool _isChanged;

    public TextChoices(IOptions<WebFormsCoreOptions>? options = null, ILogger<TextChoices>? logger = null)
    {
        _options = options;
        _logger = logger;
    }

    public ICollection<string> Values
    {
        get
        {
            if (_values != null)
            {
                return _values;
            }

            if (_json != null)
            {
                try
                {
                    _values = JsonSerializer.Deserialize(_json, JsonContext.Default.ICollectionString);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, "Failed to deserialize choices");
                }
            }

            _values ??= new List<string>();

            return _values;
        }
    }

    protected override async ValueTask OnPreRenderAsync(CancellationToken token)
    {
        if (_values != null)
        {
            var json = JsonSerializer.Serialize(_values, JsonContext.Default.ICollectionString);
            _isChanged = !string.Equals(_json ?? "[]", json, StringComparison.Ordinal);
            _json = json;
        }

        await base.OnPreRenderAsync(token);
    }

    public override async ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        var hiddenClass = _options?.Value.HiddenClass;

        if (_isChanged)
        {
            writer.AddAttribute("data-value", _json);
        }

        writer.AddAttribute(HtmlTextWriterAttribute.Class, "js-choice choices__inner");
        writer.AddAttribute("data-wfc-ignore", null);

        if (IsReadOnly)
        {
            writer.AddAttribute("data-wfc-disabled", null);
        }

        if (AutoPostBack)
        {
            writer.AddAttribute("data-wfc-autopostback", null);
        }

        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Div);
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Name, ClientID);

            if (hiddenClass is not null)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, hiddenClass);
            }
            else
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Style, "display:none;");
            }

            writer.AddAttribute(HtmlTextWriterAttribute.Type, "text");
            await writer.RenderSelfClosingTagAsync(HtmlTextWriterTag.Input);

            await writer.WriteAsync("<input class=\"choices__input js-choice-temp\">");
        }
        await writer.RenderEndTagAsync();
    }

    private ValueTask<bool> LoadPostDataAsync(string postDataKey, IFormCollection postCollection, CancellationToken cancellationToken)
    {
        if (IsReadOnly || !postCollection.TryGetValue(postDataKey, out var value))
        {
            return new ValueTask<bool>(false);
        }

        var isChanged = !string.Equals(_json ?? "[]", value, StringComparison.Ordinal);
        _isChanged = isChanged;
        _json = value;
        _values = null;

        return new ValueTask<bool>(ValuesChanged != null && isChanged);
    }

    protected virtual ValueTask RaisePostDataChangedEventAsync(CancellationToken cancellationToken)
    {
        return ValuesChanged?.InvokeAsync(this, EventArgs.Empty) ?? default;
    }

    protected virtual Task RaisePostBackEventAsync(string? eventArgument)
    {
        return Task.CompletedTask;
    }

    Task IPostBackAsyncEventHandler.RaisePostBackEventAsync(string? eventArgument)
        => RaisePostBackEventAsync(eventArgument);

    ValueTask<bool> IPostBackAsyncDataHandler.LoadPostDataAsync(string postDataKey, IFormCollection postCollection, CancellationToken cancellationToken)
        => LoadPostDataAsync(postDataKey, postCollection, cancellationToken);

    ValueTask IPostBackAsyncDataHandler.RaisePostDataChangedEventAsync(CancellationToken cancellationToken)
        => RaisePostDataChangedEventAsync(cancellationToken);

    string IValidateableControl.GetValidationValue() => string.Join(",", Values);
}