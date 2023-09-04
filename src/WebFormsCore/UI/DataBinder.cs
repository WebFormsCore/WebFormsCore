using System;

namespace WebFormsCore.UI;

public static class DataBinder
{
    public static object? Eval(object? item, string dataField)
    {
        if (item == null)
        {
            return null;
        }

        var type = item.GetType();
        var property = type.GetProperty(dataField);

        if (property == null)
        {
            throw new InvalidOperationException($"Property {dataField} not found on type {type}");
        }

        return property.GetValue(item);
    }
}
