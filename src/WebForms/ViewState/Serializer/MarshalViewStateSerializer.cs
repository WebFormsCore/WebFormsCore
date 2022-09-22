using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Web.Serializer;

public class MarshalViewStateSerializer<T> : ViewStateSerializer<T>
    where T : struct
{
    private static readonly int Size = Unsafe.SizeOf<T>();

    public override bool TryWrite(T value, Span<byte> span, out int length)
    {
        length = Size;

        if (span.Length < length)
        {
            return false;
        }

        MemoryMarshal.Write(span, ref value);
        return true;
    }

    public override T Read(ReadOnlySpan<byte> span, out int length)
    {
        length = Size;
        return MemoryMarshal.Read<T>(span);
    }
}
