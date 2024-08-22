using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WebFormsCore.UI;
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
		using var response = await httpClient.GetAsync("https://jsonplaceholder.typicode.com/todos");
		var todos = await response.Content.ReadFromJsonAsync(TodoJsonContext.Default.IAsyncEnumerableTodoModel);

		if (todos is null)
		{
			return;
		}

		if (e.FilterByKeys)
		{
			var keys = sender.Keys.GetAll<int>(nameof(TodoModel.Id));

			todos = todos.Where(x => keys.Contains(x.Id));
		}

		list.SetDataSource(todos);
	}

	protected Task Delete_OnClick(LinkButton sender, EventArgs e)
	{
		var item = sender.FindDataItem<TodoModel>();

		return Task.CompletedTask;
	}
}
