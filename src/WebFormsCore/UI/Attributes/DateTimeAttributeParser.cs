using System;
using System.Globalization;

namespace WebFormsCore.UI.Attributes;

public class DateTimeAttributeParser : IAttributeParser<DateTime>
{
    public bool SupportsRouteConstraint(string name) => name == "datetime";

    public DateTime Parse(string value)
    {
        return DateTime.Parse(value, CultureInfo.InvariantCulture);
    }
}
