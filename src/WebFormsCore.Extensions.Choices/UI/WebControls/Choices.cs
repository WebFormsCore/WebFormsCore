using System.Text.Json;
using HttpStack.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebFormsCore.UI.WebControls;

public partial class Choices : Control, IPostBackAsyncDataHandler, IPostBackAsyncEventHandler
{
    private readonly IOptions<WebFormsCoreOptions> _options;

    [ViewState(nameof(SaveTextViewState))] private string? _json;

    [ViewState] public bool IsReadOnly { get; set; }

    [ViewState] public bool AutoPostBack { get; set; }

    [ViewState] public bool Multiple { get; set; }

    [ViewState] public List<ListItem> Items { get; private set; } = new();

    protected virtual bool SaveTextViewState => (ValuesChanged != null || IsReadOnly);

    public event AsyncEventHandler<Choices, EventArgs>? ValuesChanged;

    private readonly ILogger<Choices>? _logger;
    private ICollection<string>? _values;

    public Choices(IOptions<WebFormsCoreOptions> options, ILogger<Choices>? logger = null)
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

    protected override void OnInit(EventArgs args)
    {
        base.OnInit(args);

        Page.ClientScript.RegisterStartupStyleLink(typeof(Choices), "Choices", "https://cdn.jsdelivr.net/npm/choices.js/public/assets/styles/choices.min.css");
        Page.ClientScript.RegisterStartupScriptLink(typeof(Choices), "Choices", "https://cdn.jsdelivr.net/npm/choices.js/public/assets/scripts/choices.min.js");
        Page.ClientScript.RegisterStartupScript(typeof(Choices), "ChoicesStartup", """
            WebFormsCore.bind(".js-choice", {
                init: function(element) {
                    const input = element.querySelector('input,select');
                    const choice = new Choices(input, {
                        allowHTML: true,
                        removeItemButton: true
                    });

                    element.classList.remove('choices__inner');
                    element.input = input;
                    element.choice = choice;

                    input.addEventListener('change', function () {
                        if (element.autoPostBack) {
                            WebFormsCore.postBackChange(input, 50);
                        }
                    });
                },
                update: function(element, newElement) {
                    const { choice, input } = element;
                    
                    // Auto post back
                    element.autoPostBack = newElement.hasAttribute('data-wfc-autopostback');

                    // Set disabled
                    if (newElement.hasAttribute('data-wfc-disabled')) {
                        choice.disable();
                    } else {
                        choice.enable();
                    }

                    // Set value
                    const json = newElement.getAttribute('data-value');

                    if (json) {
                        const values = JSON.parse(json);

                        if (input.tagName === 'INPUT') {
                            choice.clearStore();
                            choice.setValue(values);
                        } else {
                            choice.removeActiveItems();
                            choice.setChoiceByValue(values);
                        }
                    }
                    
                    return true;
                },
                submit: function(element, data) {
                    const { choice, input } = element;

                    data.set(input.name, JSON.stringify(choice.getValue(true)));
                },
                destroy: function(element) {
                    const { choice } = element;

                    choice.destroy();
                }
            });
            """);
    }

    protected override void OnPreRender(EventArgs args)
    {
        if (Page.Csp.Enabled)
        {
            if (_options.Value.HiddenClass is null) Page.Csp.StyleSrc.AddUnsafeInlineHash("display:none;");
            Page.Csp.ImgSrc.Add("data:");
        }

        if (_values != null)
        {
            _json = JsonSerializer.Serialize(_values, JsonContext.Default.ICollectionString);
        }

        base.OnPreRender(args);
    }

    public override async ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        var hiddenClass = _options.Value.HiddenClass;

        if (_values is not null)
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

            if (Items.Count > 0)
            {
                if (hiddenClass is not null)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, hiddenClass);
                }
                else
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Style, "display:none;");
                }

                if (Multiple)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Multiple, null);
                }

                await writer.RenderBeginTagAsync(HtmlTextWriterTag.Select);
                {
                    foreach (var item in Items)
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Value, item.Value);

                        if (Values.Contains(item.Value))
                        {
                            writer.AddAttribute(HtmlTextWriterAttribute.Selected, null);
                        }

                        if (!item.Enabled)
                        {
                            writer.AddAttribute(HtmlTextWriterAttribute.Disabled, null);
                        }

                        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Option);
                        {
                            await writer.WriteEncodedTextAsync(item.Text);
                        }
                        await writer.RenderEndTagAsync();
                    }
                }
                await writer.RenderEndTagAsync();
            }
            else
            {
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
            }
        }
        await writer.RenderEndTagAsync();
    }

    protected virtual ValueTask<bool> LoadPostDataAsync(string postDataKey, IFormCollection postCollection, CancellationToken cancellationToken)
    {
        if (IsReadOnly || !postCollection.TryGetValue(postDataKey, out var value))
        {
            return new ValueTask<bool>(false);
        }

        if (!Multiple && !string.IsNullOrEmpty(value))
        {
            value = $"[{value}]";
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
