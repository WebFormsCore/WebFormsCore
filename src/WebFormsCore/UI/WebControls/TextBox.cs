using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using HttpStack.Collections;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI.WebControls;

public partial class TextBox : WebControl, IPostBackAsyncEventHandler, IPostBackAsyncDataHandler
{
    [ViewState(nameof(SaveTextViewState))] private string? _text;
    private bool _changedText;

    public TextBox()
        : base(HtmlTextWriterTag.Input)
    {
    }

    [ViewState] public bool AutoPostBack { get; set; }

    [ViewState] public bool ReadOnly { get; set; }

    [ViewState] public TextBoxMode TextMode { get; set; }

    [ViewState] public int MaxLength { get; set; }

    [ViewState] public AutoCompleteType AutoCompleteType { get; set; }

    [ViewState(nameof(IsMultiLine))] public virtual bool Wrap { get; set; } = true;

    [ViewState(nameof(IsMultiLine))] public int Columns { get; set; }

    [ViewState(nameof(IsMultiLine))] public int Rows { get; set; }

    public string? Text
    {
        get => _text;
        set
        {
            _text = value;
            _changedText = true;
        }
    }

    public event AsyncEventHandler? TextChanged;

    public event AsyncEventHandler? EnterPressed;

    private bool IsMultiLine => TextMode == TextBoxMode.MultiLine;

    private bool IsReadOnly => !IsEnabled || !Visible || ReadOnly;

    protected virtual bool SaveTextViewState => TextMode != TextBoxMode.Password && (TextChanged != null || IsReadOnly || GetType() != typeof (TextBox));

    protected override async Task AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        await base.AddAttributesToRender(writer, token);

        writer.AddAttribute(HtmlTextWriterAttribute.Name, UniqueID);

        if (!Page.IsPostBack || _changedText)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Value, _text ?? string.Empty);
        }

        if (MaxLength > 0) writer.AddAttribute(HtmlTextWriterAttribute.Maxlength, MaxLength.ToString(CultureInfo.InvariantCulture));

        switch (AutoCompleteType)
        {
            case AutoCompleteType.None:
                // ignore.
                break;
            case AutoCompleteType.Disabled:
                writer.AddAttribute(HtmlTextWriterAttribute.AutoComplete, "off");
                break;
            case AutoCompleteType.Enabled:
                writer.AddAttribute(HtmlTextWriterAttribute.AutoComplete, "on");
                break;
            default:
                if (TextMode == TextBoxMode.SingleLine)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.VCardName, GetVCardAttributeValue(AutoCompleteType));
                }
                break;
        }

        switch (TextMode)
        {
            case TextBoxMode.MultiLine:
                writer.AddAttribute(HtmlTextWriterAttribute.Cols, Columns.ToString(CultureInfo.InvariantCulture));
                writer.AddAttribute(HtmlTextWriterAttribute.Rows, Rows.ToString(CultureInfo.InvariantCulture));
                if (!Wrap) writer.AddAttribute(HtmlTextWriterAttribute.Wrap, "off");
                break;
            default:
                writer.AddAttribute(HtmlTextWriterAttribute.Type, GetTypeAttributeValue(TextMode));
                break;
        }

        if (TextChanged != null) writer.AddAttribute("data-wfc-autopostback", null);
    }

    protected override void SetAttribute(string name, string? value)
    {
        if (name.Equals("name", StringComparison.OrdinalIgnoreCase))
        {
            // ignore
        }
        else if (name.Equals("value", StringComparison.OrdinalIgnoreCase))
        {
            Text = value;
        }
        else
        {
            base.SetAttribute(name, value);
        }
    }

    private static string GetTypeAttributeValue(TextBoxMode mode)
    {
        return mode switch
        {
            TextBoxMode.SingleLine => "text",
            TextBoxMode.Password => "password",
            TextBoxMode.Color => "color",
            TextBoxMode.Date => "date",
            TextBoxMode.DateTime => "datetime",
            TextBoxMode.DateTimeLocal => "datetime-local",
            TextBoxMode.Email => "email",
            TextBoxMode.Month => "month",
            TextBoxMode.Number => "number",
            TextBoxMode.Range => "range",
            TextBoxMode.Search => "search",
            TextBoxMode.Phone => "tel",
            TextBoxMode.Time => "time",
            TextBoxMode.Url => "url",
            TextBoxMode.Week => "week",
            _ => throw new InvalidOperationException()
        };
    }

    private static string GetVCardAttributeValue(AutoCompleteType type)
    {
        switch (type)
        {
            case AutoCompleteType.None:
            case AutoCompleteType.Disabled:
            case AutoCompleteType.Enabled:
                throw new InvalidOperationException();
            case AutoCompleteType.HomeCountryRegion:
                return "HomeCountry";
            case AutoCompleteType.BusinessCountryRegion:
                return "BusinessCountry";
            case AutoCompleteType.Search:
                return "search";
            default:
                var str = Enum.Format(typeof (AutoCompleteType), type, "G");
                if (str.StartsWith("Business", StringComparison.Ordinal))
                    str = str.Insert(8, ".");
                else if (str.StartsWith("Home", StringComparison.Ordinal))
                    str = str.Insert(4, ".");
                return "vCard." + str;
        }
    }

    protected virtual ValueTask<bool> LoadPostDataAsync(string postDataKey, IFormCollection postCollection, CancellationToken cancellationToken)
    {
        if (IsReadOnly || !postCollection.TryGetValue(postDataKey, out var value))
        {
            return new ValueTask<bool>(false);
        }

        var isChanged = !string.Equals(_text, value, StringComparison.Ordinal);
        _text = value;

        return new ValueTask<bool>(TextChanged != null && isChanged);
    }

    protected virtual ValueTask RaisePostDataChangedEventAsync(CancellationToken cancellationToken)
    {
        if (TextChanged != null)
        {
            return TextChanged.InvokeAsync(this, EventArgs.Empty);
        }

        return default;
    }

    protected virtual async Task RaisePostBackEventAsync(string? eventArgument)
    {
        if (eventArgument == "ENTER")
        {
            await EnterPressed.InvokeAsync(this, EventArgs.Empty);
        }
    }

    Task IPostBackAsyncEventHandler.RaisePostBackEventAsync(string? eventArgument)
        => RaisePostBackEventAsync(eventArgument);

    ValueTask<bool> IPostBackAsyncDataHandler.LoadPostDataAsync(string postDataKey, IFormCollection postCollection, CancellationToken cancellationToken)
        => LoadPostDataAsync(postDataKey, postCollection, cancellationToken);

    ValueTask IPostBackAsyncDataHandler.RaisePostDataChangedEventAsync(CancellationToken cancellationToken)
        => RaisePostDataChangedEventAsync(cancellationToken);
}
