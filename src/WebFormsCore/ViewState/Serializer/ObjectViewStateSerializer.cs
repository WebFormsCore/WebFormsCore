using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore.Serializer;
#if NET
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
public class ObjectViewStateSerializer : ViewStateSerializer<object>
{
    private readonly Dictionary<byte, IViewStateSerializer> _serializers;

    public ObjectViewStateSerializer(IServiceProvider provider, IEnumerable<ViewStateSerializerRegistration> registrations)
    {
        _serializers = registrations.ToDictionary(
            i => i.Id,
            i => (IViewStateSerializer)provider.GetRequiredService(i.SerializerType));
    }

    public override bool TryWrite(object? value, Span<byte> span, out int length)
    {
        if (value is null)
        {
            span[0] = 0;
            length = 1;
            return true;
        }

        var type = value.GetType();
        KeyValuePair<byte, IViewStateSerializer> registration = default;

        foreach (var kv in _serializers)
        {
            if (kv.Value.CanSerialize(type))
            {
                registration = kv;
                break;
            }
        }

        if (registration.Key == 0)
        {
            throw new InvalidOperationException($"No serializer found for type {type}");
        }

        span[0] = registration.Key;
        span = span.Slice(1);

        if (!registration.Value.TryWrite(value, span, out length))
        {
            return false;
        }

        length += 1;
        return true;
    }

    public override object? Read(ReadOnlySpan<byte> span, out int length)
    {
        var id = span[0];

        if (id == 0)
        {
            length = 1;
            return null;
        }

        if (!_serializers.TryGetValue(id, out var serializer))
        {
            throw new InvalidOperationException($"No serializer found for id {id}");
        }

        span = span.Slice(1);
        var value = serializer.Read(span, out length);

        length += 1;
        return value;
    }
}
