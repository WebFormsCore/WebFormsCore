using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebFormsCore.Example.Controls;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Example;

public partial class StreamTest : Page, IPostBackAsyncLoadHandler
{
	[ViewState] public bool ShowClock { get; set; }

	public Task AfterPostBackLoadAsync()
	{
		if (ShowClock)
		{
			return AddClockAsync();
		}

		return Task.CompletedTask;
	}

	private async Task AddClockAsync()
	{
		ShowClock = true;

		var clock = LoadControl<Clock>();
		var panel = LoadControl<StreamPanel>();
		await panel.Controls.AddAsync(clock);

		await Controls.AddAsync(panel);
	}

	protected async Task btnStartStream_OnClick(LinkButton sender, EventArgs e)
	{
		await AddClockAsync();
	}
}
