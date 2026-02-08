using System;

namespace WebFormsCore.UI.Attributes;

public class GuidAttributeParser : IAttributeParser<Guid>
{
    public bool SupportsRouteConstraint(string name) => name == "guid";

    public Guid Parse(string value)
    {
        return Guid.Parse(value);
    }
}
