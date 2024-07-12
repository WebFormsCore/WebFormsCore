#pragma warning disable IL2057
#pragma warning disable IL2026

using System;

namespace WebFormsCore.UI.Attributes;

public class TypeAttributeParser : IAttributeParser<Type>
{
    public Type Parse(string value)
    {
        var type = Type.GetType(value, false);

        if (type == null)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(value);

                if (type != null)
                {
                    break;
                }
            }
        }

        if (type == null)
        {
            throw new InvalidOperationException($"Type {value} not found");
        }

        return type;
    }
}
