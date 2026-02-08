namespace WebFormsCore.UI.Attributes;

public class Int32AttributeParser : IAttributeParser<int>
{
    public bool SupportsRouteConstraint(string name) => name == "int";

    public int Parse(string value)
    {
        return int.Parse(value);
    }
}