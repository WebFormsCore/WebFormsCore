using System;
using System.Collections.Generic;

namespace WebFormsCore;

public interface IControlTypeProvider
{
	System.Collections.Generic.Dictionary<string, System.Type> GetTypes();
}
