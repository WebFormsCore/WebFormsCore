using System.Globalization;

namespace WebFormsCore.UI.Attributes;

public class DoubleAttributeParser : IAttributeParser<double>
{
    public bool SupportsRouteConstraint(string name) => name == "double";

    public double Parse(string value)
    {
        return double.Parse(value, CultureInfo.InvariantCulture);
    }
}
