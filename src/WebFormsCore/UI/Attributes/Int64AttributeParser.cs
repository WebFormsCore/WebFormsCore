namespace WebFormsCore.UI.Attributes;

public class Int64AttributeParser : IAttributeParser<long>
{
    public bool SupportsRouteConstraint(string name) => name == "long";

    public long Parse(string value)
    {
        return long.Parse(value);
    }
}
