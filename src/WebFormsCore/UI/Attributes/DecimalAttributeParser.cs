using System.Globalization;

namespace WebFormsCore.UI.Attributes;

public class DecimalAttributeParser : IAttributeParser<decimal>
{
    public bool SupportsRouteConstraint(string name) => name == "decimal";

    public decimal Parse(string value)
    {
        return decimal.Parse(value, CultureInfo.InvariantCulture);
    }
}
