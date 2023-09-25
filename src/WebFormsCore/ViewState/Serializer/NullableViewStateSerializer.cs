using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace WebFormsCore.Serializer;

public class NullableViewStateSerializer : IViewStateSerializer
{
    public bool CanSerialize(Type type)
    {
        return IsSupported(type, out _);
    }

    public void Write(Type type, ref ViewStateWriter writer, object? value, object? defaultValue)
    {
        if (value is null)
        {
            writer.WriteByte(0);
            return;
        }

        writer.WriteByte(1);

        if (!IsSupported(type, out var typeArgument))
        {
            throw new InvalidOperationException("Invalid type");
        }

        writer.WriteObject(typeArgument, value, defaultValue);
    }

    public object? Read([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ref ViewStateReader reader, object? defaultValue)
    {
        var hasValue = reader.ReadByte();

        if (hasValue == 0)
        {
            return null;
        }

        if (!IsSupported(type, out var typeArgument))
        {
            throw new InvalidOperationException("Invalid type");
        }

        return reader.ReadObject(typeArgument, defaultValue);
    }

    public bool StoreInViewState(Type type, object? value, object? defaultValue)
    {
        return !Equals(value, defaultValue);
    }

    public void TrackViewState(Type type, object? value, ViewStateProvider provider)
    {
        if (!IsSupported(type, out var typeArgument))
        {
            throw new InvalidOperationException("Invalid type");
        }

        provider.TrackViewState(typeArgument, value);
    }

    private static bool IsSupported(Type type, [NotNullWhen(true)] out Type? typeArgument)
    {
        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();

            if (definition == typeof(Nullable<>))
            {
                typeArgument = type.GetGenericArguments()[0];
                return true;
            }
        }

        typeArgument = null;
        return false;
    }
}
