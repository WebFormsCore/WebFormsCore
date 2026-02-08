namespace WebFormsCore.UI.Attributes;

public class StringAttributeParser : IAttributeParser<string>
{
    public bool SupportsRouteConstraint(string name) => false;

    public string Parse(string value)
    {
        return value;
    }
}