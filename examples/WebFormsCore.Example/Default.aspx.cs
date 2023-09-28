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
        Csp.Enabled = true;
        // EnablePageViewState = false;

        await phTodoContainer.Controls.AddAsync(
            LoadControl("Controls/TodoList.ascx")
        );

        if (!IsPostBack)
        {
            await grid.DataBindAsync();
        }
    }

    protected override void OnLoad(EventArgs args)
    {
        title.InnerText = (PostbackCount++).ToString();

        editor.Text = editor.Text.ToUpper();
    }

    protected Task choices_OnValuesChanged(Choices sender, EventArgs e)
    {
        choices.Values.Remove("1");
        return Task.CompletedTask;
    }

    protected Task textChoice_OnValuesChanged(TextChoices sender, EventArgs e)
    {
        textChoice.Values.Remove("1");
        return Task.CompletedTask;
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

    protected async Task grid_OnNeedDataSource(Grid sender, NeedDataSourceEventArgs e)
    {
        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync("https://jsonplaceholder.typicode.com/todos");
        var todos = await response.Content.ReadFromJsonAsync(TodoJsonContext.Default.IAsyncEnumerableTodoModel);

        if (todos is null)
        {
            return;
        }

        if (e.FilterByKeys)
        {
            var keys = sender.Keys.GetAll<int>("Id");

            todos = todos.Where(x => keys.Contains(x.Id));
        }

        await grid.LoadDataSourceAsync(todos);
    }

    protected Task btnRedirect_OnClick(Button sender, EventArgs e)
    {
        Response.StatusCode = 302;
        Response.Headers["Location"] = "https://www.example.com";
        return Task.CompletedTask;
    }
}

public record TodoModel(
    [property: JsonPropertyName("userId")] int UserId,
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("completed")] bool Completed
);

[JsonSerializable(typeof(IAsyncEnumerable<TodoModel>))]
public partial class TodoJsonContext : JsonSerializerContext
{

}