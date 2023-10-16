using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Features;

namespace WebFormsCore.UI;

public partial class ClientDependencyExclude : Control
{
	public ClientDependencyType DependencyType { get; internal set; }

	/// <summary>Name of the script (e.g. <c>jQuery</c>, <c>Bootstrap</c>, <c>Angular</c>, etc.</summary>
	[ViewState] public string? Name { get; set; }

	protected override void OnInit(EventArgs args)
	{
		base.OnInit(args);

		var service = Context.RequestServices.GetService<ClientDependencyCollection>();
		var item = service?.Files.FirstOrDefault(x => x.DependencyType == DependencyType &&
		                                              string.Equals(x.Name, Name, StringComparison.OrdinalIgnoreCase));

		if (item != null && service != null)
		{
			service.Files.Remove(item);
		}
	}
}
