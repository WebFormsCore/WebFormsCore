using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebFormsCore.UI.WebControls;

public partial class DropDownList() : WebControl(HtmlTextWriterTag.Select), IPostBackAsyncEventHandler, IPostBackAsyncDataHandler, IValidateableControl, ICausesValidationControl, IPostBackLoadHandler
{
    [ViewState(nameof(SaveTextViewState))] private int _selectedIndex;
    [ViewState] private string? _validationGroup;

    [ViewState] public bool CausesValidation { get; set; } = true;

    [ViewState] public List<ListItem> Items { get; set; } = new();

    public string ValidationGroup
    {
        get => _validationGroup ?? Page.GetDefaultValidationGroup(this);
        set => _validationGroup = value;
    }

    [ViewState] public bool AutoPostBack { get; set; }

    public int SelectedIndex
    {
        get
        {
            var span = CollectionsMarshal.AsSpan(Items);

            for (var i = 0; i < span.Length; i++)
            {
                if (span[i].Selected)
                {
                    return i;
                }
            }

            return span.Length == 0 ? -1 : 0;
        }
        set
        {
            if (value < -1 || value >= Items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            ClearSelection();

            if (value >= 0)
            {
                Items[value].Selected = true;
                _selectedIndex = value;
            }
            else
            {
                _selectedIndex = -1;
            }
        }

    }

    public string SelectedValue
    {
        get
        {
            var span = CollectionsMarshal.AsSpan(Items);

            foreach (var item in span)
            {
                if (item.Selected)
                {
                    return item.Value;
                }
            }

            if (span.Length > 0)
            {
                return span[0].Value;
            }

            return string.Empty;
        }
        set => SetSelectedValue(value);
    }

    private void SetSelectedValue(string value, bool updateSelectedIndex = true)
    {
        ClearSelection();

        var span = CollectionsMarshal.AsSpan(Items);

        for (var i = 0; i < span.Length; i++)
        {
            if (!string.Equals(span[i].Value, value, StringComparison.Ordinal))
            {
                continue;
            }

            span[i].Selected = true;
            if (updateSelectedIndex)
            {
                _selectedIndex = i;
            }
            return;
        }
    }

    public void ClearSelection()
    {
        foreach (var item in Items)
        {
            item.Selected = false;
        }
    }

    public event AsyncEventHandler<DropDownList, EventArgs>? SelectedIndexChanged;

    protected virtual bool SaveTextViewState => SelectedIndexChanged != null || !IsEnabled || !Visible;

    protected override async ValueTask AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        await base.AddAttributesToRender(writer, token);

        writer.AddAttribute(HtmlTextWriterAttribute.Name, UniqueID);
        if (AutoPostBack) writer.AddAttribute("data-wfc-autopostback", null);

        if (CausesValidation)
        {
            writer.AddAttribute("data-wfc-validate", ValidationGroup);
        }
    }

    protected override async ValueTask RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
    {
        foreach (var item in Items)
        {
            if (item.Selected)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Selected, "selected");
            }

            if (!item.Enabled)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Disabled, null);
            }

            if (item.HasAttributes)
            {
                item.Attributes.AddAttributes(writer);
            }

            writer.AddAttribute(HtmlTextWriterAttribute.Value, item.Value);

            await writer.RenderBeginTagAsync(HtmlTextWriterTag.Option);
            await writer.WriteEncodedTextAsync(item.Text);
            await writer.RenderEndTagAsync();
        }
    }

    protected virtual ValueTask<bool> LoadPostDataAsync(string postDataKey, IFormCollection postCollection, CancellationToken cancellationToken)
    {
        if (!IsEnabled || !postCollection.TryGetValue(postDataKey, out var value))
        {
            return new ValueTask<bool>(false);
        }

        SetSelectedValue(value.ToString(), updateSelectedIndex: false);

        var isChanged = SelectedIndex != _selectedIndex;
        _selectedIndex = SelectedIndex;

        return new ValueTask<bool>(SelectedIndexChanged != null && isChanged);
    }

    protected virtual ValueTask RaisePostDataChangedEventAsync(CancellationToken cancellationToken)
    {
        if (SelectedIndexChanged != null)
        {
            return SelectedIndexChanged.InvokeAsync(this, EventArgs.Empty);
        }

        return default;
    }

    protected virtual async Task RaisePostBackEventAsync(string? eventArgument)
    {
        if (CausesValidation)
        {
            if (!await Page.ValidateAsync(ValidationGroup))
            {
                return;
            }
        }
    }

    Task IPostBackAsyncEventHandler.RaisePostBackEventAsync(string? eventArgument)
        => RaisePostBackEventAsync(eventArgument);

    ValueTask<bool> IPostBackAsyncDataHandler.LoadPostDataAsync(string postDataKey, IFormCollection postCollection, CancellationToken cancellationToken)
        => LoadPostDataAsync(postDataKey, postCollection, cancellationToken);

    ValueTask IPostBackAsyncDataHandler.RaisePostDataChangedEventAsync(CancellationToken cancellationToken)
        => RaisePostDataChangedEventAsync(cancellationToken);

    string? IValidateableControl.GetValidationValue() => SelectedValue;

    void IPostBackLoadHandler.AfterPostBackLoad()
    {
        ClearSelection();

        var index = _selectedIndex;
        var span = CollectionsMarshal.AsSpan(Items);

        if (index >= 0 && index < span.Length)
        {
            span[index].Selected = true;
        }
    }
}
