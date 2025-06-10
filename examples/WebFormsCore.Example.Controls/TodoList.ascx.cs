﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Example.Controls;

public partial class TodoList : Control
{
    [ViewState] public TextBoxMode? TextMode { get; set; }

    protected override void OnLoad(EventArgs args)
    {
        base.OnLoad(args);

        TextMode = TextBoxMode.MultiLine;
    }

    protected async Task tbItem_OnEnterPressed(object? sender, EventArgs e)
    {
        if (tbItem.Text is {} value)
        {
            await rptItems.AddAsync(value);
            tbItem.Text = "";
        }
    }

    protected void rptItems_OnItemDataBound(object? sender, RepeaterItemEventArgs<string> e)
    {
        if (e.Item.DataItem is not { } dataItem) return;

        var controls = e.Item.FindControls<ItemControls>();

        controls.litValue.Text = dataItem;
    }

    protected void btnRemove_OnClick(object? sender, EventArgs e)
    {
        var button = (Button) sender!;
        var item = (RepeaterItem<string>) button.NamingContainer!;

        rptItems.Remove(item);
    }

    protected Task tbItem_Validate(CustomValidator sender, ServerValidateEventArgs e)
    {
        if (string.Equals(e.Value, "todo", StringComparison.OrdinalIgnoreCase))
        {
            e.IsValid = false;
        }

        return Task.CompletedTask;
    }
}
