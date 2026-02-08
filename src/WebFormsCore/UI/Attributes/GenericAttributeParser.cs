using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace WebFormsCore.UI.Attributes;

internal sealed class GenericAttributeParser<T> : IAttributeParser<T>
{
    private readonly IAttributeParser<T> _inner;

    [SuppressMessage("Trimming", "IL2090", Justification = "We are checking the type constraints at runtime.")]
    [SuppressMessage("Trimming", "IL2091", Justification = "We are checking the type constraints at runtime.")]
    [SuppressMessage("Trimming", "IL3050", Justification = "We are checking the type constraints at runtime.")]
    public GenericAttributeParser(IServiceProvider serviceProvider)
    {
        var type = typeof(T);

        if (type.IsEnum)
        {
            _inner = (IAttributeParser<T>)Activator.CreateInstance(typeof(EnumAttributeParser<>).MakeGenericType(type))!;
        }
        else if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            var parserType = typeof(ArrayAttributeParser<>).MakeGenericType(elementType);
            var baseParserType = typeof(IAttributeParser<>).MakeGenericType(elementType);
            var baseParser = serviceProvider.GetRequiredService(baseParserType);
            _inner = (IAttributeParser<T>)Activator.CreateInstance(parserType, baseParser)!;
        }
        else if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>) ||
                                       type.GetGenericTypeDefinition() == typeof(IList<>) ||
                                       type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)))
        {
            var elementType = type.GetGenericArguments()[0];

            if (type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
            {
                var parserType = typeof(ArrayAttributeParser<>).MakeGenericType(elementType);
                var baseParserType = typeof(IAttributeParser<>).MakeGenericType(elementType);
                var baseParser = serviceProvider.GetRequiredService(baseParserType);
                _inner = (IAttributeParser<T>)Activator.CreateInstance(parserType, baseParser)!;
            }
            else
            {
                var parserType = typeof(ListAttributeParser<>).MakeGenericType(elementType);
                var arrayParserType = typeof(IAttributeParser<>).MakeGenericType(elementType.MakeArrayType());
                var arrayParser = serviceProvider.GetRequiredService(arrayParserType);
                _inner = (IAttributeParser<T>)Activator.CreateInstance(parserType, arrayParser)!;
            }
        }
        else
        {
            throw new NotSupportedException($"Type {type.FullName} is not supported by GenericAttributeParser");
        }
    }

    public bool SupportsRouteConstraint(string name) => _inner.SupportsRouteConstraint(name);

    public T Parse(string value) => _inner.Parse(value);
}
