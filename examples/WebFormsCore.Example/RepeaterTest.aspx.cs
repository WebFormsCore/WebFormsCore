using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Example;

public partial class RepeaterTest : Page
{
	protected override async ValueTask OnLoadAsync(CancellationToken token)
	{
		await base.OnLoadAsync(token);

		if (!IsPostBack)
		{
			await list.DataBindAsync(token);
		}
	}

	protected async Task OnNeedDataSource(Repeater sender, NeedDataSourceEventArgs e)
	{
		using var httpClient = new HttpClient();

		var url = "https://jsonplaceholder.typicode.com/todos";

		if (e.FilterByPage(out var info))
		{
			url += $"?_start={info.Offset}&_limit={info.PageSize}";
		}

		using var response = await httpClient.GetAsync(url);
		var todos = await response.Content.ReadFromJsonAsync(TodoJsonContext.Default.IAsyncEnumerableTodoModel);

		if (todos is null)
		{
			return;
		}

		list.SetDataSource(todos);
	}

	protected Task Delete_OnClick(LinkButton sender, EventArgs e)
	{
		var item = sender.FindDataItem<TodoModel>();

		return Task.CompletedTask;
	}
}
