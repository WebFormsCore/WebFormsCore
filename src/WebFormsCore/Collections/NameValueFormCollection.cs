using System.Collections.Specialized;
using HttpStack.Collections;

namespace WebFormsCore.Collections;


public class NameValueFormCollection : NameValueDictionary, IFormCollection
{
	public NameValueFormCollection(NameValueCollection nameValueCollection, IFormFileCollection files)
	{
		SetNameValueCollection(nameValueCollection);
		Files = files;
	}

	public IFormFileCollection Files { get; private set; }

	public override void Reset()
	{
		base.Reset();

		if (Files is FormFileCollection collection)
		{
			collection.Reset();
		}
		else
		{
			Files = new FormFileCollection();
		}
	}
}
