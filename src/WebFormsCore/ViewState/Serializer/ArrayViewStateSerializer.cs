using System;
using System.Collections.Generic;

namespace WebFormsCore.Serializer;

public class ArrayViewStateSerializer<T> : FixedLengthViewStateSerializer<T[]>
    where T : notnull
{
    private readonly IViewStateSerializer<T> _serializer;

    public ArrayViewStateSerializer(IViewStateSerializer<T> serializer)
    {
        _serializer = serializer;
    }

    protected override bool IsDefaultValue(T[] value) => value.Length == 0;

    protected override T[] GetDefaultValue() => Array.Empty<T>();

    protected override int WriteValue(T[] value, Span<byte> span)
    {
        var totalLength = 0;

        foreach (var item in value)
        {
            if (!_serializer.TryWrite(item, span, out var length))
            {
                throw new InvalidOperationException("Could not write item");
            }

            span = span.Slice(length);
            totalLength += length;
        }

        return totalLength;
    }

    protected override T[] ReadValue(ReadOnlySpan<byte> span)
    {
        var list = new List<T>();

        while (span.Length > 0)
        {
            var value = _serializer.Read(span, out var length);
            span = span.Slice(length);
            list.Add(value!);
        }

        return list.ToArray();
    }

    protected override bool TryGetLengthImpl(T[] value, out int length)
    {
        length = 0;

        foreach (var item in value)
        {
            if (!_serializer.TryGetLength(item, out var itemLength))
            {
                return false;
            }

            length += itemLength;
        }

        return true;
    }
}
