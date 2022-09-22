using System;
using System.ComponentModel;
using System.Globalization;

namespace WebFormsCore.UI;

internal class MinimizableAttributeTypeConverter : BooleanConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        if (sourceType == typeof(string))
        {
            return true;
        }

        return base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string strValue)
        {
            return strValue.Length > 0 && !string.Equals(strValue, "false", StringComparison.OrdinalIgnoreCase);
        }

        return base.ConvertFrom(context, culture, value);
    }
}
