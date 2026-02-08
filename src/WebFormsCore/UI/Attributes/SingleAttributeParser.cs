using System.Globalization;

namespace WebFormsCore.UI.Attributes;

public class SingleAttributeParser : IAttributeParser<float>
{
    public bool SupportsRouteConstraint(string name) => name == "float";

    public float Parse(string value)
    {
        return float.Parse(value, CultureInfo.InvariantCulture);
    }
}
