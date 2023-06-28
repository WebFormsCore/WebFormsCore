using System.Text.Json;
using HttpStack.Collections;
using Microsoft.Extensions.Logging;

namespace WebFormsCore.UI.WebControls;

public partial class Choices : Control, IPostBackAsyncDataHandler, IPostBackAsyncEventHandler
{
    [ViewState(nameof(SaveTextViewState))] private string? _json;

    [ViewState] public bool IsReadOnly { get; set; }

    [ViewState] public bool AutoPostBack { get; set; }

    [ViewState] public List<ListItem> Items { get; set; } = new();

    protected virtual bool SaveTextViewState => (ValuesChanged != null || IsReadOnly);

    public event AsyncEventHandler? ValuesChanged;

    private readonly ILogger<Choices>? _logger;
    private List<string>? _values;

    public Choices(ILogger<Choices>? logger = null)
    {
        _logger = logger;
    }

    public List<string> Values
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
                    _values = JsonSerializer.Deserialize<List<string>>(_json);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, "Failed to deserialize choices");
                }
            }

            _values ??= new List<string>();

            return _values;
        }
        set => _values = value;
    }

    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);

        Page.ClientScript.RegisterStartupStyleLink(typeof(Choices), "Choices", "https://cdn.jsdelivr.net/npm/choices.js/public/assets/styles/choices.min.css");
        Page.ClientScript.RegisterStartupScriptLink(typeof(Choices), "Choices", "https://cdn.jsdelivr.net/npm/choices.js/public/assets/scripts/choices.min.js");
        Page.ClientScript.RegisterStartupScript(typeof(Choices), "ChoicesStartup", """
            WebFormsCore.bind(".js-choice", {
                init: function(element) {
                    const input = element.querySelector('input');
                    const choice = new Choices(input);

                    element.input = input;
                    element.choice = choice;

                    input.addEventListener('change', function (e) {
                        if (element.hasAttribute('data-wfc-autopostback')) {
                            WebFormsCore.postBackChange(input, 50);
                        }
                    });
                },
                update: function(element) {
                    const { choice } = element;

                    // Set disabled
                    if (element.hasAttribute('data-wfc-disabled')) {
                        choice.disable();
                    } else {
                        choice.enable();
                    }

                    // Set value
                    const json = element.getAttribute('data-value');

                    if (json) {
                        const values = JSON.parse(json);
                        choice.clearStore();
                        choice.setValue(values);
                    }
                },
                submit: function(element, data) {
                    const { choice, input } = element;

                    data.append(input.name, JSON.stringify(choice.getValue(true)));
                }
            });
            """);
    }

    protected override void OnPreRender(EventArgs args)
    {
        if (_values != null)
        {
            _json = JsonSerializer.Serialize(_values);
        }

        base.OnPreRender(args);
    }

    public override async Task RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (_values is not null)
        {
            writer.AddAttribute("data-value", _json);
        }

        writer.AddAttribute(HtmlTextWriterAttribute.Class, "js-choice");

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
            writer.AddAttribute("data-wfc-ignore", "");
            await writer.RenderBeginTagAsync(HtmlTextWriterTag.Div);
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Name, ClientID);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "choices__inner"); // TODO: Fix padding

                await writer.RenderBeginTagAsync(HtmlTextWriterTag.Input);
                await writer.RenderEndTagAsync();
            }
            await writer.RenderEndTagAsync();
        }

        await writer.RenderEndTagAsync();
    }

    protected virtual ValueTask<bool> LoadPostDataAsync(string postDataKey, IFormCollection postCollection, CancellationToken cancellationToken)
    {
        if (IsReadOnly || !postCollection.TryGetValue(postDataKey, out var value))
        {
            return new ValueTask<bool>(false);
        }

        var isChanged = !string.Equals(_json ?? "[]", value, StringComparison.Ordinal);
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
}
