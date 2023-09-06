using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using HttpStack;
using WebFormsCore.Example.Controls;
using WebFormsCore.Security;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Example;

public partial class Default : Page
{
    [ViewState] private List<string> _items = new();

    [ViewState] public int PostbackCount { get; set; }

    protected override async Task OnInitAsync(CancellationToken token)
    {
        Csp.Enabled = true;
        // EnablePageViewState = false;

        await phTodoContainer.Controls.AddAsync(
            LoadControl("Controls/TodoList.ascx")
        );
    }

    protected override void OnLoad(EventArgs args)
    {
    }

    protected override async Task OnLoadAsync(CancellationToken token)
    {
        title.InnerText = (PostbackCount++).ToString();

        if (!IsPostBack)
        {
            await grid.LoadDataSourceAsync(new[]
            {
                new { Id = 1, Name = "Foo", IsNew = true },
                new { Id = 2, Name = "Bar", IsNew = false },
            });
        }
    }

    protected void choices_OnValuesChanged(object? sender, EventArgs e)
    {
        choices.Values.Remove("1");
    }

    protected Task cb_OnCheckedChanged(object? sender, EventArgs e)
    {
        litCb.Text = cb.Checked.ToString(CultureInfo.InvariantCulture);
        return Task.CompletedTask;
    }

    protected async Task btnDownload_OnClick(object? sender, EventArgs e)
    {
        Response.ContentType = "application/octet-stream";
        Response.Headers["Content-Disposition"] = "attachment; filename=\"foo.txt\"";
        await Response.WriteAsync("Hello World");
    }
}
