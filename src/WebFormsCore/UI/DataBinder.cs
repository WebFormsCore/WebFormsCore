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
        if (item == null || string.IsNullOrEmpty(dataField))
        {
            return null;
        }

        if (!dataField.Contains('.'))
        {
            return EvalInternal(item, dataField);
        }

        object? current = item;
        foreach (var field in dataField.Split('.'))
        {
            if (current == null)
            {
                return null;
            }

            current = EvalInternal(current, field);
        }

        return current;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:DynamicallyAccessedMembers", Justification = "We are using reflection to access properties for DataBinding. The user is responsible for ensuring the properties are available.")]
    private static object? EvalInternal(object item, string dataField)
    {
        var property = item.GetType().GetProperty(dataField);

        if (property == null)
        {
            throw new InvalidOperationException($"Property {dataField} not found on type {item.GetType()}");
        }

        return property.GetValue(item);
    }
}
