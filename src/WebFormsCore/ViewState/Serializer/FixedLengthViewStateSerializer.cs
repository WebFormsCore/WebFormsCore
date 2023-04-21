using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WebFormsCore.Serializer;

public abstract class FixedLengthViewStateSerializer<T> : ViewStateSerializer<T>
    where T : notnull
{
    public const int Offset = 2;

    protected virtual bool IsDefaultValue(T value) => false;

    protected virtual T GetDefaultValue() => throw new InvalidOperationException("No default value");

    protected abstract int WriteValue(T value, Span<byte> span);

    protected abstract T ReadValue(ReadOnlySpan<byte> span);

    public override bool TryWrite(T? value, Span<byte> span, out int length)
    {
        ushort prefix;

        if (value is null)
        {
            prefix = 0;
            length = 2;
        }
        else if (IsDefaultValue(value))
        {
            prefix = 1;
            length = 2;
        }
        else
        {
            var valueLength = WriteValue(value, span.Slice(2));
            prefix = (ushort)(valueLength + Offset);
            length = valueLength + 2;
        }

        MemoryMarshal.Write(span.Slice(0, 2), ref prefix);

        return true;
    }

    public override T? Read(ReadOnlySpan<byte> span, out int length)
    {
        var prefix = MemoryMarshal.Read<ushort>(span.Slice(0, 2));

        if (prefix == 0)
        {
            length = 2;
            return default;
        }

        if (prefix == 1)
        {
            length = 2;
            return GetDefaultValue();
        }

        length = prefix - Offset + 2;
        return ReadValue(span.Slice(2, prefix - Offset));
    }

    protected abstract bool TryGetLengthImpl(T value, out int length);

    public override bool TryGetLength(T? value, out int length)
    {
        if (value is null)
        {
            length = 2;
            return true;
        }

        if (IsDefaultValue(value))
        {
            length = 2;
            return true;
        }

        if (!TryGetLengthImpl(value, out length))
        {
            return false;
        }

        length += 2;
        return true;
    }
}
