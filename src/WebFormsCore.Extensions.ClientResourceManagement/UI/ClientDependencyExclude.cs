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

		if (Name is not null)
		{
			var service = Context.RequestServices.GetService<IClientDependencyCollection>();

			service?.Remove(DependencyType, Name);
		}
	}
}
