using System.Collections.Generic;

namespace WebFormsCore.UI.Attributes;

public class ListAttributeParser<T> : IAttributeParser<List<T>>
    where T : notnull
{
    private readonly IAttributeParser<T[]> _parser;

    public ListAttributeParser(IAttributeParser<T[]> parser)
    {
        _parser = parser;
    }

    public List<T> Parse(string value)
    {
        return new List<T>(_parser.Parse(value));
    }
}