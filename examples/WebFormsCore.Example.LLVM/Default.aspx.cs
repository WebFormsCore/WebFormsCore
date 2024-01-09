using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HttpStack;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Example;

public partial class Default : Page
{
    [ViewState] private List<string> _items = new();

    [ViewState] public int PostbackCount { get; set; }

    protected override async ValueTask OnInitAsync(CancellationToken token)
    {
        await phTodoContainer.Controls.AddAsync(
            LoadControl("Controls/TodoList.ascx")
        );
    }

    protected override void OnLoad(EventArgs args)
    {
        title.InnerText = (PostbackCount++).ToString();
    }

    protected Task cb_OnCheckedChanged(CheckBox sender, EventArgs e)
    {
        litCb.Text = cb.Checked.ToString(CultureInfo.InvariantCulture);
        return Task.CompletedTask;
    }

    protected async Task btnDownload_OnClick(Button sender, EventArgs e)
    {
        Response.ContentType = "application/octet-stream";
        Response.Headers["Content-Disposition"] = "attachment; filename=\"foo.txt\"";
        await Response.WriteAsync("Hello World");
    }

    protected Task btnRedirect_OnClick(Button sender, EventArgs e)
    {
        Response.StatusCode = 302;
        Response.Headers["Location"] = "https://www.example.com";
        return Task.CompletedTask;
    }
}
