using System;

namespace WebFormsCore.Serializer;

public interface IViewStateSerializer
{
    bool TryWrite(object value, Span<byte> span, out int length);

    object? ReadObject(ReadOnlySpan<byte> span, out int length);
}

public interface IViewStateSerializer<T> : IViewStateSerializer
{
    bool TryWrite(T value, Span<byte> span, out int length);

    T Read(ReadOnlySpan<byte> span, out int length);
}

public abstract class ViewStateSerializer<T> : IViewStateSerializer<T>
{
    public abstract bool TryWrite(T value, Span<byte> span, out int length);

    public abstract T Read(ReadOnlySpan<byte> span, out int length);

    bool IViewStateSerializer.TryWrite(object value, Span<byte> span, out int length)
    {
        if (value is not T t)
        {
            throw new InvalidOperationException("Invalid type");
        }

        return TryWrite(t, span, out length);
    }

    object? IViewStateSerializer.ReadObject(ReadOnlySpan<byte> span, out int length)
    {
        return Read(span, out length);
    }
}