using System;
using System.Collections.Generic;

namespace WebFormsCore.UI.Attributes;

public class ArrayAttributeParser<T> : IAttributeParser<T[]>
    where T : notnull
{
    private readonly IAttributeParser<T> _parser;
    private readonly Dictionary<string, T[]> _instances = new();

    public ArrayAttributeParser(IAttributeParser<T> parser)
    {
        _parser = parser;
    }

    public bool SupportsRouteConstraint(string name)
    {
        if (!name.EndsWith("[]", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return _parser.SupportsRouteConstraint(name[..^2]);
    }

    public T[] Parse(string value)
    {
        if (_instances.TryGetValue(value, out var instance))
        {
            return instance;
        }

        var values = value.Split(',');
        var result = new T[values.Length];

        for (var i = 0; i < values.Length; i++)
        {
            result[i] = _parser.Parse(values[i].Trim());
        }

        _instances[value] = result;

        return result;
    }
}
