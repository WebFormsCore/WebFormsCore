using System;

namespace WebFormsCore.UI.Attributes;

public class TypeAttributeParser : IAttributeParser<Type>
{
    public Type Parse(string value)
    {
        return Type.GetType(value, true)!;
    }
}
