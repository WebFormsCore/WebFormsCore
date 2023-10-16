namespace WebFormsCore.UI;

public class JsExclude : ClientDependencyExclude
{
	public JsExclude()
	{
		DependencyType = ClientDependencyType.Javascript;
	}
}
