using HttpStack;
using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore.Example.LLVM;

public static class Startup
{
	public static void ConfigureServices(IServiceCollection services)
	{
	}

	public static void Configure(IHttpStackBuilder app)
	{
		app.Run(async context =>
		{
			if (context.Request.Path == "/favicon.ico")
			{
				context.Response.StatusCode = 404;
				return;
			}

			await context.ExecutePageAsync("Default.aspx");
		});
	}
}
