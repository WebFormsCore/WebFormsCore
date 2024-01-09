using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using HttpStack;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore;
using WebFormsCore.Compiler;
using WebFormsCore.UI;

public partial class Runtime
{
	public static void Main()
	{
	}

	[JSImport("window.location.href", "main.js")]
	internal static partial string GetHRef();

	[JSExport]
	public static async Task<string> Render(string text)
	{
		var loadContext = new AssemblyLoadContext("Control", isCollectible: true);

		try
		{
			var services = new ServiceCollection();

			services.AddSingleton<IWebFormsEnvironment, WasmEnvironment>();
			services.AddWebFormsCore();

			var serviceProvider = services.BuildServiceProvider();
			await using var scope = serviceProvider.CreateAsyncScope();

			await using var stream = new MemoryStream();

			var context = new InMemoryHttpContext
			{
				Response =
				{
					Body = stream
				},
				RequestServices = scope.ServiceProvider
			};

			var pageManager = serviceProvider.GetRequiredService<IPageManager>();

			var type = await CompileControl(text, loadContext);

			if (typeof(Page).IsAssignableFrom(type))
			{
				await pageManager.RenderPageAsync(context, type);
			}
			else
			{
				var webObjectActivator = scope.ServiceProvider.GetRequiredService<IWebObjectActivator>();
				var control = (Control) webObjectActivator.CreateControl(type);

				var page = new Page();
				page.Controls.AddWithoutPageEvents(control);

				await pageManager.RenderPageAsync(context, page);
			}

			stream.Position = 0;

			return Encoding.UTF8.GetString(stream.ToArray());
		}
		finally
		{
			loadContext.Unload();
		}
	}

	private static async Task<Type> CompileControl(string text, AssemblyLoadContext context)
	{
		using var assemblyStream = new MemoryStream();
		await GetReferences();

		var (compilation, designerType) = ViewCompiler.Compile(
			path: @$"C:\temp\test{Guid.NewGuid()}.aspx",
			text,
			generateHash: false,
			concurrentBuild: false,
			references: await GetReferences());

		compilation.Emit(assemblyStream);

		assemblyStream.Seek(0, SeekOrigin.Begin);

		var assembly = context.LoadFromStream(assemblyStream);

		return assembly.GetType(designerType.DesignerFullTypeName!)!;
	}

	private static List<MetadataReference>? _references;

	private static async Task<List<MetadataReference>> GetReferences()
	{
		if (_references is not null)
		{
			return _references;
		}

		var appAssemblies = new List<string>
		{
			"System.Private.CoreLib",
			"System.Runtime",
			"WebFormsCore"
		};

		using var httpClient = new HttpClient();
		var references = new List<MetadataReference>();
		var baseUri = new Uri(GetHRef());

		foreach (var assemblyName in appAssemblies)
		{
			var assemblyUrl = new Uri(baseUri, $"/_framework/{assemblyName}.dll");
			var response = await httpClient.GetAsync(assemblyUrl);

			if (!response.IsSuccessStatusCode)
			{
				continue;
			}

			var bytes = await response.Content.ReadAsStreamAsync();

			references.Add(MetadataReference.CreateFromStream(bytes));
		}

		_references = references;

		return references;
	}

	public class WasmEnvironment : IWebFormsEnvironment
	{
		public string? ContentRootPath => null;

		public bool EnableControlWatcher => false;

		public bool CompileInBackground => false;
	}
}
