using System;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.Example.Controls;
using WebFormsCore.UI;

namespace WebFormsCore.Example;

public partial class Default : Page
{
    [ViewState] public int PostbackCount { get; set; }

    protected override async Task OnInitAsync(CancellationToken token)
    {
        // Csp.Enabled = true;
        EnablePageViewState = false;

        await phTodoContainer.Controls.AddAsync(
            LoadControl("Controls/TodoList.ascx")
        );
    }

    protected override void OnLoad(EventArgs args)
    {
        title.InnerText = (PostbackCount++).ToString();
    }
}
