using System;

namespace WebFormsCore.UI.Attributes;

public class EnumAttributeParser<T> : IAttributeParser<T>
    where T : struct, Enum
{
    public T Parse(string value)
    {
        return Enum.Parse<T>(value);
    }
}
