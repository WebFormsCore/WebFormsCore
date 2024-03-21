using System;
using System.Diagnostics.CodeAnalysis;

namespace WebFormsCore.UI;

public static class DataBinder
{
    public static object? Eval<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(T? item, string dataField)
    {
        if (item == null)
        {
            return null;
        }

        var property = typeof(T).GetProperty(dataField);

        if (property == null)
        {
            throw new InvalidOperationException($"Property {dataField} not found on type {typeof(T)}");
        }

        return property.GetValue(item);
    }
}
