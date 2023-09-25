using System.Text.Json;
using HttpStack.Collections;
using Microsoft.Extensions.Options;
using WebFormsCore.UI.WebControls.Internal;

namespace WebFormsCore.UI.WebControls;

public partial class Choices : ChoicesBase, IPostBackAsyncDataHandler, IPostBackAsyncEventHandler, IValidateableControl
{
    private readonly IOptions<WebFormsCoreOptions>? _options;
    private ListItemValues? _values;

    [ViewState] public bool IsReadOnly { get; set; }

    [ViewState] public bool AutoPostBack { get; set; }

    [ViewState] public bool Multiple { get; set; }

    [ViewState] public List<ListItem> Items { get; private set; } = new();

    public ListItemValues Values => _values ??= new ListItemValues(Items);

    public event AsyncEventHandler<Choices, EventArgs>? ValuesChanged;

    public Choices(IOptions<WebFormsCoreOptions>? options = null)
    {
        _options = options;
    }

    public override async ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        var hiddenClass = _options?.Value.HiddenClass;

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

            if (Multiple)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Multiple, null);
            }

            await writer.RenderBeginTagAsync(HtmlTextWriterTag.Select);
            {
                foreach (var item in Items)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Value, item.Value);

                    if (item.Selected)
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
        await writer.RenderEndTagAsync();
    }

    private ValueTask<bool> LoadPostDataAsync(string postDataKey, IFormCollection postCollection)
    {
        if (IsReadOnly || !postCollection.TryGetValue(postDataKey, out var value))
        {
            return new ValueTask<bool>(false);
        }

        if (!Multiple && !string.IsNullOrEmpty(value))
        {
            value = $"[{value}]";
        }

        var items = JsonSerializer.Deserialize(value, JsonContext.Default.StringArray) ?? Array.Empty<string>();
        var isEqual = Values.SequenceEqual(items);

        if (!isEqual)
        {
            foreach (var item in Items)
            {
                item.Selected = items.Contains(item.Value);
            }
        }

        return new ValueTask<bool>(ValuesChanged != null && !isEqual);
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
        => LoadPostDataAsync(postDataKey, postCollection);

    ValueTask IPostBackAsyncDataHandler.RaisePostDataChangedEventAsync(CancellationToken cancellationToken)
        => RaisePostDataChangedEventAsync(cancellationToken);

    string IValidateableControl.GetValidationValue() => Values.ToString();
}
