namespace WebFormsCore.UI.Attributes;

public class Int16AttributeParser : IAttributeParser<short>
{
    public bool SupportsRouteConstraint(string name) => false;

    public short Parse(string value)
    {
        return short.Parse(value);
    }
}
