using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace WebFormsCore.Serializer;

public class TypeViewStateSerializer : ViewStateSerializer<Type>
{
    private readonly ConcurrentDictionary<string, Type> _cachedTypes = new();

    public override void Write(Type type, ref ViewStateWriter writer, Type? value, Type? defaultValue)
    {
        if (value is null)
        {
            writer.Write((string?)null);
            return;
        }

        var name = value.ToString();
        var assemblyName = value.Assembly.GetName().Name;

        if (assemblyName is null)
        {
            writer.Write(name);
            return;
        }

        var length = name.Length + assemblyName.Length + 2;

        Span<char> span = stackalloc char[length];

        name.AsSpan().CopyTo(span);
        span[name.Length] = ',';
        span[name.Length + 1] = ' ';
        assemblyName.AsSpan().CopyTo(span.Slice(name.Length + 2));

        writer.WriteSpan(span);

        _cachedTypes.TryAdd(span.ToString(), value);
    }

    public override Type? Read([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ref ViewStateReader reader, Type? defaultValue)
    {
        var value = reader.Read<string>();

        if (value is null)
        {
            return defaultValue;
        }

        if (_cachedTypes.TryGetValue(value, out var typeValue))
        {
            return typeValue;
        }

#pragma warning disable IL2057
        return Type.GetType(value);
#pragma warning restore IL2057
    }

    public override bool StoreInViewState(Type type, Type? value, Type? defaultValue)
    {
        return value != defaultValue;
    }

    public override void TrackViewState(Type type, Type? value, ViewStateProvider provider)
    {
    }
}
