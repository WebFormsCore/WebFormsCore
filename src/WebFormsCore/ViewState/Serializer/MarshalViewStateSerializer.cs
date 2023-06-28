using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WebFormsCore.Serializer;

#if NET
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
public class MarshalViewStateSerializer<T> : ViewStateSerializer<T>
    where T : struct
{
    private static readonly int Size = Unsafe.SizeOf<T>();

    public override void Write(Type type, ref ViewStateWriter writer, T value, T defaultValue)
    {
        MemoryMarshal.Write(writer.AllocateUnsafe(Size), ref value);
    }

    public override T Read(Type type, ref ViewStateReader reader, T defaultValue)
    {
        return MemoryMarshal.Read<T>(reader.ReadBytes(Size));
    }

    public override bool StoreInViewState(Type type, T value, T defaultValue)
    {
        return true;
    }
}
