using System;

namespace WebFormsCore.UI.Attributes;

public class TypeAttributeParser : IAttributeParser<Type>
{
    public Type Parse(string value)
    {
#pragma warning disable IL2057
        return Type.GetType(value, true)!;
#pragma warning restore IL2057
    }
}
