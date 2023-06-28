using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.Example.Controls;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Example;

public partial class Default : Page
{
    [ViewState] private List<string> _items = new();

    [ViewState] public int PostbackCount { get; set; }

    protected override async Task OnInitAsync(CancellationToken token)
    {
        // Csp.Enabled = true;
        // EnablePageViewState = false;

        await phTodoContainer.Controls.AddAsync(
            LoadControl("Controls/TodoList.ascx")
        );
    }

    protected override void OnLoad(EventArgs args)
    {
        title.InnerText = (PostbackCount++).ToString();
    }

    protected Task choices_OnValuesChanged(object? sender, EventArgs e)
    {
        return Task.CompletedTask;
    }
}
