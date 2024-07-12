using System;
using System.Diagnostics.CodeAnalysis;

namespace WebFormsCore.UI;

public static class DataBinder
{
    public static object? GetDataItem(object container)
    {
        while (true)
        {
            if (container is IDataItemContainer dataItemContainer)
            {
                return dataItemContainer.DataItem;
            }

            if (container is not Control control)
            {
                return null;
            }

            if (control.ParentInternal is null)
            {
                return null;
            }

            container = control.ParentInternal;
        }
    }

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
