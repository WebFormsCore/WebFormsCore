using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore.Serializer;

public class DefaultViewStateSerializer : IDefaultViewStateSerializer
{
    public const int Offset = 1;

    private readonly Dictionary<byte, IViewStateSerializer> _serializers;

    public DefaultViewStateSerializer(IServiceProvider provider, IEnumerable<ViewStateSerializerRegistration> registrations)
    {
        _serializers = registrations.ToDictionary(
            i => i.Id,
            i => (IViewStateSerializer)provider.GetRequiredService(i.SerializerType));
    }

    protected virtual object WriteFallback(Type type, ref ViewStateWriter writer, object value, object? defaultValue)
    {
        throw new InvalidOperationException($"No serializer found for type {type.FullName}");
    }

    protected virtual object? ReadFallback(Type type, ref ViewStateReader reader, object? defaultValue)
    {
        throw new InvalidOperationException($"No serializer found for type {type.FullName}");
    }

    public virtual void Write(Type type, ref ViewStateWriter writer, object? value, object? defaultValue)
    {
        if (value is null)
        {
            writer.WriteByte(0);
            return;
        }

        if (!TryGetRegistration(type, out var registration))
        {
            WriteFallback(type, ref writer, value, defaultValue);
            return;
        }

        writer.WriteByte(registration.Key);
        registration.Value.Write(type, ref writer, value, defaultValue);
    }

    public object? Read(Type type, ref ViewStateReader reader, object? defaultValue)
    {
        var id = reader.ReadByte();

        if (id == 0)
        {
            return null;
        }

        if (!TryGetRegistration(type, out var registration))
        {
            return ReadFallback(type, ref reader, defaultValue);
        }

        return registration.Value.Read(type, ref reader, defaultValue);
    }

    public bool StoreInViewState(Type type, object? value, object? defaultValue)
    {
        if (value is null) return defaultValue != null;
        if (defaultValue is null) return true;

        if (!TryGetRegistration(type, out var registration))
        {
            return StoreInViewStateFallback(type, value, defaultValue);
        }

        return registration.Value.StoreInViewState(type, value, defaultValue);
    }

    protected virtual bool StoreInViewStateFallback(Type type, object value, object defaultValue)
    {
        return true;
    }

    private bool TryGetRegistration(Type type, out KeyValuePair<byte, IViewStateSerializer> registration)
    {
        registration = default;

        foreach (var kv in _serializers)
        {
            if (kv.Value.CanSerialize(type))
            {
                registration = kv;
                return true;
            }
        }

        return false;
    }

    bool IViewStateSerializer.CanSerialize(Type type) => true;
}
