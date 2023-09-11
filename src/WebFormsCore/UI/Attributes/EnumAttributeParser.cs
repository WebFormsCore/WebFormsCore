using System;

namespace WebFormsCore.UI.Attributes;

public class EnumAttributeParser<T> : IAttributeParser<T>
    where T : struct, Enum
{
    public T Parse(string value)
    {
#if NET
        return Enum.Parse<T>(value);
#else
        return (T)Enum.Parse(typeof(T), value);
#endif
    }
}
