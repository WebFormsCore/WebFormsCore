using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WebFormsCore.Serializer;

public class EnumViewStateSerializer : IViewStateSerializer
{
    public bool CanSerialize(Type type)
    {
        return type.IsEnum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Write<T>(ref ViewStateWriter writer, object value)
        where T : struct
    {
        var span = writer.AllocateUnsafe(Unsafe.SizeOf<T>());
        var unbox = Unsafe.Unbox<T>(value);

        MemoryMarshal.Write(span, in unbox);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Read<T>(ref ViewStateReader reader)
        where T : struct
    {
        var span = reader.ReadBytes(Unsafe.SizeOf<T>());

        return MemoryMarshal.Read<T>(span);
    }

    public void Write(Type type, ref ViewStateWriter writer, object? value, object? defaultValue)
    {
        if (value is null)
        {
            throw new InvalidOperationException("Enum value cannot be null");
        }

        var underlyingType = Enum.GetUnderlyingType(type);

        if (underlyingType == typeof(byte))
        {
            Write<byte>(ref writer, value);
        }
        else if (underlyingType == typeof(sbyte))
        {
            Write<sbyte>(ref writer, value);
        }
        else if (underlyingType == typeof(short))
        {
            Write<short>(ref writer, value);
        }
        else if (underlyingType == typeof(ushort))
        {
            Write<ushort>(ref writer, value);
        }
        else if (underlyingType == typeof(int))
        {
            Write<int>(ref writer, value);
        }
        else if (underlyingType == typeof(uint))
        {
            Write<uint>(ref writer, value);
        }
        else if (underlyingType == typeof(long))
        {
            Write<long>(ref writer, value);
        }
        else if (underlyingType == typeof(ulong))
        {
            Write<ulong>(ref writer, value);
        }
        else
        {
            throw new InvalidOperationException($"Unexpected underlying type {underlyingType.FullName}");
        }
    }

    public object? Read([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ref ViewStateReader reader, object? defaultValue)
    {
        var underlyingType = Enum.GetUnderlyingType(type);

        if (underlyingType == typeof(byte))
        {
            return Enum.ToObject(type, Read<byte>(ref reader));
        }

        if (underlyingType == typeof(sbyte))
        {
            return Enum.ToObject(type, Read<sbyte>(ref reader));
        }

        if (underlyingType == typeof(short))
        {
            return Enum.ToObject(type, Read<short>(ref reader));
        }

        if (underlyingType == typeof(ushort))
        {
            return Enum.ToObject(type, Read<ushort>(ref reader));
        }

        if (underlyingType == typeof(int))
        {
            return Enum.ToObject(type, Read<int>(ref reader));
        }

        if (underlyingType == typeof(uint))
        {
            return Enum.ToObject(type, Read<uint>(ref reader));
        }

        if (underlyingType == typeof(long))
        {
            return Enum.ToObject(type, Read<long>(ref reader));
        }

        if (underlyingType == typeof(ulong))
        {
            return Enum.ToObject(type, Read<ulong>(ref reader));
        }

        throw new InvalidOperationException($"Unexpected underlying type {underlyingType.FullName}");
    }

    public bool StoreInViewState(Type type, object? value, object? defaultValue)
    {
        return !Equals(value, defaultValue);
    }

    public void TrackViewState(Type type, object? value, ViewStateProvider provider)
    {
    }
}
